<?php
/**
 * ProThailand Master Server - Production PHP Endpoint (api.php)
 * Deploy this file to https://api.prothailand.net/api.php
 */

header('Content-Type: application/json; charset=utf-8');

$masterPassword = "config.sys.input=UTF8";
$iterations = 100000;

function decryptPayload($b64Data, $password, $iterations) {
    $raw = base64_decode($b64Data);
    if (strlen($raw) < 48) {
        return false;
    }

    $salt = substr($raw, 0, 16);
    $iv   = substr($raw, 16, 16);
    $ct   = substr($raw, 32);

    // Derive 256-bit key using PBKDF2-HMAC-SHA256
    $key = hash_pbkdf2("sha256", $password, $salt, $iterations, 32, true);

    // Decrypt AES-256-CBC
    $decrypted = openssl_decrypt($ct, "aes-256-cbc", $key, OPENSSL_RAW_DATA, $iv);
    if ($decrypted === false) {
        return false;
    }

    // Strip UTF-8 BOM if present
    return preg_replace('/^\xEF\xBB\xBF/', '', $decrypted);
}

// Check parameters
$sv   = isset($_GET['sv']) ? $_GET['sv'] : '';
$info = isset($_GET['info']) ? $_GET['info'] : '';
$data = isset($_GET['data']) ? $_GET['data'] : '';

if (empty($data)) {
    echo json_encode([
        "Server" => "ProThailand Master API (Production)",
        "Status" => "ONLINE",
        "Endpoint" => "https://api.prothailand.net/api.php"
    ], JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE);
    exit;
}

$payload = decryptPayload($data, $masterPassword, $iterations);
if ($payload === false) {
    echo json_encode([
        "Status" => "ERROR",
        "Message" => "Invalid encrypted payload"
    ]);
    exit;
}

// Payload format:
// username | passHash | hdd | mb | mac | bios | tag | osUser | osMachine | version
$parts = explode('|', $payload);
$username   = isset($parts[0]) ? $parts[0] : '';
$passHash   = isset($parts[1]) ? $parts[1] : '';
$hddSerial  = isset($parts[2]) ? $parts[2] : '';
$mbSerial   = isset($parts[3]) ? $parts[3] : '';
$macAddr    = isset($parts[4]) ? $parts[4] : '';
$biosUuid   = isset($parts[5]) ? $parts[5] : '';
$clientTag  = isset($parts[6]) ? $parts[6] : '';
$osUser     = isset($parts[7]) ? $parts[7] : '';
$osMachine  = isset($parts[8]) ? $parts[8] : '';
$clientVer  = isset($parts[9]) ? $parts[9] : '';

// --- CUSTOM DATABASE OR AUTHENTICATION LOGIC HERE ---
// For demonstration, we log HWID and return LOGINOK
// In production, query MySQL / PostgreSQL database:
// e.g.:
// $stmt = $pdo->prepare("SELECT * FROM users WHERE username = ?");
// $stmt->execute([$username]);
// $user = $stmt->fetch();

$response = [
    "Status" => "LOGINOK",
    "Username" => $username,
    "Rank" => "Driver",
    "Credits" => 1000,
    "Hardware" => [
        "MAC"  => $macAddr,
        "MB"   => $mbSerial,
        "BIOS" => $biosUuid,
        "HDD"  => $hddSerial
    ],
    "ServerMessage" => "Welcome " . $username . "! Authenticated via ProThailand Master Server."
];

echo json_encode($response, JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE);
?>
