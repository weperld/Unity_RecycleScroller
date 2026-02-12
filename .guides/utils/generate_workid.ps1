# WorkID 자동 생성 스크립트 (PowerShell)
# 새로운 WorkID를 생성합니다.

function Get-LastWorkID {
    param(
        [string]$WorkInProgressPath
    )

    try {
        $content = Get-Content $WorkInProgressPath -Raw -Encoding UTF8

        # WIP-YYYYMMDD-NNN 형식 찾기
        $matches = [regex]::Matches($content, "WIP-(\d{8})-(\d{3})")

        if ($matches.Count -eq 0) {
            return $null, 0
        }

        # 가장 최신 WorkID 찾기 (날짜 + 숫자 기준)
        $latest = $matches | Sort-Object {
            $_.Groups[1].Value, $_.Groups[2].Value
        } -Descending | Select-Object -First 1

        $date = $latest.Groups[1].Value
        $num = [int]$latest.Groups[2].Value

        return $date, $num
    }
    catch {
        Write-Host "Error reading WORK_IN_PROGRESS.md: $_" -ForegroundColor Red
        return $null, 0
    }
}

function New-WorkID {
    param(
        [string]$WorkInProgressPath
    )

    $today = Get-Date -Format "yyyyMMdd"
    $lastDate, $lastNum = Get-LastWorkID -WorkInProgressPath $WorkInProgressPath

    if ($lastDate -eq $today) {
        # 같은 날짜면 숫자 증가
        $newNum = $lastNum + 1
    }
    else {
        # 다른 날짜면 1부터 시작
        $newNum = 1
    }

    return "WIP-$today-$($newNum.ToString('000'))"
}

function Main {
    # 현재 디렉토리에서 WORK_IN_PROGRESS.md 찾기
    $scriptPath = $MyInvocation.MyCommand.Path
    $scriptDir = Split-Path $scriptPath -Parent
    $projectDir = Split-Path (Split-Path $scriptDir -Parent) -Parent
    $workInProgressPath = Join-Path $projectDir "WORK_IN_PROGRESS.md"

    if (-not (Test-Path $workInProgressPath)) {
        Write-Host "Error: WORK_IN_PROGRESS.md not found at $workInProgressPath" -ForegroundColor Red
        return 1
    }

    # 새로운 WorkID 생성
    $newWorkID = New-WorkID -WorkInProgressPath $workInProgressPath
    $workIDParts = $newWorkID.Split('-')

    Write-Host "✅ New WorkID: $newWorkID" -ForegroundColor Green
    Write-Host "📝 Location: $workInProgressPath" -ForegroundColor Cyan
    Write-Host "📅 Date: $($workIDParts[1])" -ForegroundColor Yellow
    Write-Host "🔢 Number: $($workIDParts[2])" -ForegroundColor Yellow

    # WORK_IN_PROGRESS.md에 추가 (선택 사항)
    Write-Host "`n📌 다음 명령으로 WORK_IN_PROGRESS.md를 업데이트하세요:" -ForegroundColor Cyan
    Write-Host "에이전트: '$newWorkID 생성하고 WORK_IN_PROGRESS.md에 추가해줘'"

    return 0
}

Main
