/**
 * ProThailand Full System Cloudflare Worker
 * รวม: Web Portal + API + LFS Master Bridge Emulation
 */

const MASTER_PASSWORD = "config.sys.input=UTF8";
const ITERATIONS = 100000;

// ==================== UTILITY FUNCTIONS ====================
function base64ToUint8Array(base64Str) {
    const binStr = atob(base64Str);
    const bytes = new Uint8Array(binStr.length);
    for (let i = 0; i < binStr.length; i++) {
        bytes[i] = binStr.charCodeAt(i);
    }
    return bytes;
}

async function decryptPayload(b64Data) {
    const raw = base64ToUint8Array(b64Data);
    if (raw.length < 48) {
        throw new Error("Payload too short (minimum 48 bytes required)");
    }

    const salt = raw.subarray(0, 16);
    const iv = raw.subarray(16, 32);
    const ciphertext = raw.subarray(32);

    const pwKey = await crypto.subtle.importKey(
        "raw",
        new TextEncoder().encode(MASTER_PASSWORD),
        { name: "PBKDF2" },
        false,
        ["deriveKey"]
    );

    const aesKey = await crypto.subtle.deriveKey(
        {
            name: "PBKDF2",
            salt: salt,
            iterations: ITERATIONS,
            hash: "SHA-256"
        },
        pwKey,
        { name: "AES-CBC", length: 256 },
        false,
        ["decrypt"]
    );

    const decryptedBuffer = await crypto.subtle.decrypt(
        { name: "AES-CBC", iv: iv },
        aesKey,
        ciphertext
    );

    return new TextDecoder().decode(decryptedBuffer).replace(/^\uFEFF/, "");
}

async function hashPassword(password) {
    const data = new TextEncoder().encode(password);
    const hashBuffer = await crypto.subtle.digest("SHA-256", data);
    return Array.from(new Uint8Array(hashBuffer))
        .map(b => b.toString(16).padStart(2, "0"))
        .join("");
}

// ==================== USER DATABASE (KV) ====================
async function getUser(env, username) {
    const key = username.toLowerCase().trim();
    if (env.USERS_KV) {
        const val = await env.USERS_KV.get(key);
        return val ? JSON.parse(val) : null;
    }
    // Fallback to in-memory (for testing)
    return null;
}

async function saveUser(env, username, data) {
    const key = username.toLowerCase().trim();
    if (env.USERS_KV) {
        await env.USERS_KV.put(key, JSON.stringify(data));
    }
}

async function bindHwid(env, username, hwidData) {
    const user = await getUser(env, username);
    if (!user) return false;
    
    user.hwid = hwidData;
    user.lastLogin = new Date().toISOString();
    await saveUser(env, username, user);
    return true;
}

// ==================== LFS PROTOCOL EMULATION ====================
// LFS Master Server Binary Protocol Handler
async function handleLfsProtocol(request, env) {
    // This would handle TCP connections from LFS.exe
    // For HTTP emulation, we simulate the protocol over HTTP
    
    const url = new URL(request.url);
    const username = url.searchParams.get("user") || "";
    
    if (!username) {
        return new Response(JSON.stringify({
            status: "ERROR",
            message: "Username required"
        }), { status: 400 });
    }
    
    const user = await getUser(env, username);
    
    // LFS expects binary response but we return JSON for bridge
    return new Response(JSON.stringify({
        type: "LFS_MASTER_RESPONSE",
        authorized: !!user,
        username: username,
        license: user ? "S3" : "NONE",
        timestamp: Date.now()
    }));
}

// ==================== MAIN REQUEST HANDLER ====================
export default {
    async fetch(request, env, ctx) {
        const url = new URL(request.url);
        const path = url.pathname;
        
        // CORS headers for all responses
        const corsHeaders = {
            "Access-Control-Allow-Origin": "*",
            "Access-Control-Allow-Methods": "GET, POST, OPTIONS",
            "Access-Control-Allow-Headers": "Content-Type",
            "Content-Type": "application/json"
        };
        
        // Handle OPTIONS preflight
        if (request.method === "OPTIONS") {
            return new Response(null, { headers: corsHeaders });
        }
        
        // ============ API ENDPOINTS ============
        
        // 1. Root/Status
        if (path === "/" || path === "/api.php") {
            const dataParam = url.searchParams.get("data");
            
            if (!dataParam) {
                return new Response(JSON.stringify({
                    Server: "ProThailand Master API (Full System)",
                    Status: "ONLINE",
                    Version: "2.0.0",
                    Features: ["Web Portal", "LFS Auth", "HWID Binding", "User Management"],
                    Endpoints: {
                        web: "/",
                        api: "/api.php",
                        register: "/api/register",
                        login: "/api/login",
                        user: "/api/user",
                        lfs: "/api/lfs"
                    }
                }, null, 2), { headers: corsHeaders });
            }
            
            // Handle encrypted auth from ProThailand.exe
            try {
                const decrypted = await decryptPayload(dataParam);
                const parts = decrypted.split('|');
                const username = parts[0] || "";
                const passHash = parts[1] || "";
                const hdd = parts[2] || "";
                const mb = parts[3] || "";
                const mac = parts[4] || "";
                const bios = parts[5] || "";
                const tag = parts[6] || "";
                const osUser = parts[7] || "";
                const osMachine = parts[8] || "";
                const version = parts[9] || "";
                
                // Verify user
                const user = await getUser(env, username);
                const valid = user && user.passHash === passHash;
                
                // Bind HWID on successful login
                if (valid) {
                    await bindHwid(env, username, {
                        mac: mac,
                        mb: mb,
                        bios: bios,
                        hdd: hdd,
                        os: `${osUser}@${osMachine}`,
                        lastLogin: new Date().toISOString()
                    });
                }
                
                return new Response(JSON.stringify({
                    Status: valid ? "LOGINOK" : "WRONGPASS",
                    Username: username,
                    Valid: valid,
                    Rank: valid ? "Driver" : "Guest",
                    Credits: valid ? 1000 : 0,
                    Hardware: {
                        MAC: mac,
                        MB: mb,
                        BIOS: bios,
                        HDD: hdd
                    },
                    ServerMessage: valid 
                        ? `Welcome ${username}! Authenticated via ProThailand Cloudflare Edge.`
                        : "Authentication failed. Please check credentials."
                }, null, 2), { headers: corsHeaders });
                
            } catch (error) {
                return new Response(JSON.stringify({
                    Status: "ERROR",
                    Message: "Decryption failed: " + error.message
                }), { status: 400, headers: corsHeaders });
            }
        }
        
        // 2. User Registration
        if (path === "/api/register" && request.method === "POST") {
            try {
                const body = await request.json();
                const username = (body.username || "").trim();
                const password = body.password || "";
                const tag = (body.tag || "").trim();
                const email = (body.email || "").trim();
                
                if (!username || username.length < 3) {
                    return new Response(JSON.stringify({ 
                        success: false, 
                        message: "Username ต้องมีความยาวอย่างน้อย 3 ตัวอักษร" 
                    }), { headers: corsHeaders });
                }
                
                if (!password || password.length < 6) {
                    return new Response(JSON.stringify({ 
                        success: false, 
                        message: "Password ต้องมีความยาวอย่างน้อย 6 ตัวอักษร" 
                    }), { headers: corsHeaders });
                }
                
                // Check existing
                const existing = await getUser(env, username);
                if (existing) {
                    return new Response(JSON.stringify({ 
                        success: false, 
                        message: "Username นี้มีผู้ใช้งานในระบบแล้ว!" 
                    }), { headers: corsHeaders });
                }
                
                const passHash = await hashPassword(password);
                const newUser = {
                    username: username,
                    passHash: passHash,
                    tag: tag,
                    email: email,
                    hwid: null,
                    credits: 1000,
                    rank: "Driver",
                    createdAt: new Date().toISOString(),
                    lastLogin: null
                };
                
                await saveUser(env, username, newUser);
                
                return new Response(JSON.stringify({
                    success: true,
                    message: "สมัครสมาชิกสำเร็จ! คุณสามารถนำ Username และ Password ไปล็อกอินใน ProThailand.exe ได้ทันที",
                    user: {
                        username: username,
                        tag: tag,
                        credits: 1000
                    }
                }), { headers: corsHeaders });
                
            } catch (error) {
                return new Response(JSON.stringify({ 
                    success: false, 
                    message: "Registration error: " + error.message 
                }), { status: 400, headers: corsHeaders });
            }
        }
        
        // 3. User Lookup (for LocalMasterBridge)
        if (path === "/api/user") {
            const username = url.searchParams.get("name") || "";
            
            if (!username) {
                return new Response(JSON.stringify({
                    error: "Username parameter required"
                }), { status: 400, headers: corsHeaders });
            }
            
            const user = await getUser(env, username);
            
            return new Response(JSON.stringify({
                found: !!user,
                username: username,
                exists: !!user,
                registered: !!user,
                details: user ? {
                    username: user.username,
                    tag: user.tag,
                    credits: user.credits,
                    rank: user.rank,
                    createdAt: user.createdAt,
                    lastLogin: user.lastLogin,
                    hwidBound: !!user.hwid
                } : null
            }), { headers: corsHeaders });
        }
        
        // 4. LFS Protocol Emulation Endpoint
        if (path === "/api/lfs") {
            return handleLfsProtocol(request, env);
        }
        
        // 5. Web Portal HTML
        if (path === "/portal" || path === "/web") {
            const html = `
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="UTF-8">
                <title>ProThailand Portal</title>
                <style>
                    body { font-family: Arial, sans-serif; margin: 40px; background: #0f172a; color: white; }
                    .container { max-width: 800px; margin: 0 auto; }
                    h1 { color: #7c3aed; }
                    .card { background: #1e293b; padding: 20px; border-radius: 10px; margin: 20px 0; }
                    .btn { background: #7c3aed; color: white; padding: 10px 20px; border: none; border-radius: 5px; cursor: pointer; }
                    .form-group { margin: 15px 0; }
                    input { padding: 8px; width: 200px; border-radius: 4px; border: 1px solid #475569; background: #334155; color: white; }
                </style>
            </head>
            <body>
                <div class="container">
                    <h1>🚀 ProThailand LFS Authentication Portal</h1>
                    
                    <div class="card">
                        <h2>📝 Member Registration</h2>
                        <div class="form-group">
                            <input type="text" id="regUser" placeholder="Username" />
                        </div>
                        <div class="form-group">
                            <input type="password" id="regPass" placeholder="Password" />
                        </div>
                        <button class="btn" onclick="register()">Register</button>
                        <div id="regResult"></div>
                    </div>
                    
                    <div class="card">
                        <h2>🔍 Check User</h2>
                        <div class="form-group">
                            <input type="text" id="checkUser" placeholder="Username" />
                        </div>
                        <button class="btn" onclick="checkUser()">Check</button>
                        <div id="checkResult"></div>
                    </div>
                    
                    <div class="card">
                        <h2>📊 System Status</h2>
                        <p>API Endpoint: <code>${url.origin}/api.php</code></p>
                        <p>For ProThailand.exe use: <code>${url.origin}/api.php</code></p>
                        <p>LocalMasterBridge API: <code>${url.origin}/api/user?name=USERNAME</code></p>
                    </div>
                </div>
                
                <script>
                    async function register() {
                        const user = document.getElementById('regUser').value;
                        const pass = document.getElementById('regPass').value;
                        
                        const res = await fetch('${url.origin}/api/register', {
                            method: 'POST',
                            headers: { 'Content-Type': 'application/json' },
                            body: JSON.stringify({ username: user, password: pass })
                        });
                        
                        const data = await res.json();
                        document.getElementById('regResult').innerHTML = 
                            '<p style="color: ' + (data.success ? 'green' : 'red') + '">' + data.message + '</p>';
                    }
                    
                    async function checkUser() {
                        const user = document.getElementById('checkUser').value;
                        const res = await fetch('${url.origin}/api/user?name=' + user);
                        const data = await res.json();
                        
                        document.getElementById('checkResult').innerHTML = 
                            '<pre>' + JSON.stringify(data, null, 2) + '</pre>';
                    }
                </script>
            </body>
            </html>
            `;
            
            return new Response(html, {
                headers: { "Content-Type": "text/html; charset=utf-8" }
            });
        }
        
        // 404 - Not Found
        return new Response(JSON.stringify({
            error: "Endpoint not found",
            available: [
                "/",
                "/api.php",
                "/api/register (POST)",
                "/api/user (GET)",
                "/api/lfs (GET)",
                "/portal"
            ]
        }), { status: 404, headers: corsHeaders });
    }
};
