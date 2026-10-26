/**
 * ProThailand Master Server - Cloudflare Worker
 * Edge Serverless Endpoint for https://api.prothailand.net/api.php
 * Handles AES-256-CBC + PBKDF2 decryption via native Web Crypto API.
 */

const MASTER_PASSWORD = "config.sys.input=UTF8";
const ITERATIONS = 100000;

// Base64 to Uint8Array helper
function base64ToUint8Array(base64Str) {
    const binStr = atob(base64Str);
    const bytes = new Uint8Array(binStr.length);
    for (let i = 0; i < binStr.length; i++) {
        bytes[i] = binStr.charCodeAt(i);
    }
    return bytes;
}

// Decrypt AES-256-CBC with PBKDF2-HMAC-SHA256
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

export default {
    async fetch(request, env, ctx) {
        const url = new URL(request.url);

        // CORS headers
        const corsHeaders = {
            "Access-Control-Allow-Origin": "*",
            "Access-Control-Allow-Methods": "GET, POST, OPTIONS",
            "Access-Control-Allow-Headers": "Content-Type, Authorization",
            "Content-Type": "application/json; charset=utf-8"
        };

        if (request.method === "OPTIONS") {
            return new Response(null, { headers: corsHeaders });
        }

        // Extract encrypted payload from query param `data` or JSON body
        let dataEnc = url.searchParams.get("data");

        if (!dataEnc && request.method === "POST") {
            try {
                const body = await request.json();
                dataEnc = body.data;
            } catch (e) { }
        }

        // Status check endpoint if no data provided
        if (!dataEnc) {
            return new Response(JSON.stringify({
                Server: "ProThailand Master API (Cloudflare Worker)",
                Status: "ONLINE",
                Version: "1.0.0",
                Endpoint: "https://api.prothailand.net/api.php",
                EdgeRegion: request.cf?.colo || "Global"
            }, null, 2), { headers: corsHeaders });
        }

        try {
            // Decrypt inbound HWID payload
            const decryptedPayload = await decryptPayload(dataEnc);

            // Payload format: username|passHash|hdd|mb|mac|bios|tag|osUser|osMachine|version
            const parts = decryptedPayload.split("|");
            const username = parts[0] || "Unknown";
            const passHash = parts[1] || "";
            const hddSerial = parts[2] || "";
            const mbSerial = parts[3] || "";
            const macAddr = parts[4] || "";
            const biosUuid = parts[5] || "";
            const clientTag = parts[6] || "PT";
            const osUser = parts[7] || "";
            const osMachine = parts[8] || "";
            const clientVer = parts[9] || "1.0.0";

            // Cloudflare KV User Database check (if USERS_KV is bound)
            let userRecord = null;
            if (env.USERS_KV) {
                const kvData = await env.USERS_KV.get(username);
                if (kvData) {
                    userRecord = JSON.parse(kvData);
                } else {
                    // Auto-register first time
                    userRecord = {
                        passHash: passHash,
                        mac: macAddr,
                        mb: mbSerial,
                        bios: biosUuid,
                        createdAt: new Date().toISOString()
                    };
                    await env.USERS_KV.put(username, JSON.stringify(userRecord));
                }
            }

            // Authentication verification
            if (userRecord && userRecord.passHash !== passHash) {
                return new Response(JSON.stringify({
                    Status: "WRONGPASS",
                    Message: "Incorrect password! Please try again."
                }), { status: 200, headers: corsHeaders });
            }

            // Successful authentication response
            const authResponse = {
                Status: "LOGINOK",
                Username: username,
                Rank: "Driver",
                Credits: 1000,
                Hardware: {
                    MAC: macAddr,
                    MB: mbSerial,
                    BIOS: biosUuid,
                    HDD: hddSerial
                },
                Client: {
                    User: osUser,
                    Machine: osMachine,
                    Tag: clientTag,
                    Version: clientVer
                },
                ServerMessage: `Welcome ${username}! Authenticated successfully via Cloudflare Edge.`
            };

            return new Response(JSON.stringify(authResponse, null, 2), {
                status: 200,
                headers: corsHeaders
            });

        } catch (err) {
            return new Response(JSON.stringify({
                Status: "ERROR",
                Message: "Failed to decrypt authentication payload: " + err.message
            }), { status: 400, headers: corsHeaders });
        }
    }
};
