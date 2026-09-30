$base = "https://localhost:5001/api/v1/auth"

# --- Login: write body to a file so PowerShell never touches quoting ---
'{"email":"admin@acme.test","password":"Password123!"}' | Out-File -FilePath login.json -Encoding utf8 -NoNewline

curl.exe -k -s -D login-headers.txt -o login-body.json -X POST "$base/login" `
  -H "Content-Type: application/json" --data "@login.json"

$oldToken = (Select-String -Path login-headers.txt -Pattern 'hrms_refresh=([^;]+)').Matches[0].Groups[1].Value
Write-Output "Login status:"
Select-String -Path login-headers.txt -Pattern "^HTTP"
Write-Output "Old token: $($oldToken.Substring(0,20))..."

# --- Refresh #1: legitimate use of the token just issued ---
curl.exe -k -s -D refresh1-headers.txt -o refresh1-body.json -w "STATUS:%{http_code}`n" `
  -X POST "$base/refresh" --cookie "hrms_refresh=$oldToken"

$newToken = (Select-String -Path refresh1-headers.txt -Pattern 'hrms_refresh=([^;]+)').Matches[0].Groups[1].Value
Write-Output "New token: $($newToken.Substring(0,20))..."

# --- Replay the OLD (now-used) token -> expect 401 ---
Write-Output "`nReplaying OLD token:"
curl.exe -k -s -o replay-old.json -w "STATUS:%{http_code}`n" `
  -X POST "$base/refresh" --cookie "hrms_refresh=$oldToken"
Get-Content replay-old.json

# --- Try the NEW token -> should ALSO fail if reuse revoked the family ---
Write-Output "`nTrying NEW token after reuse attempt:"
curl.exe -k -s -o replay-new.json -w "STATUS:%{http_code}`n" `
  -X POST "$base/refresh" --cookie "hrms_refresh=$newToken"
Get-Content replay-new.json