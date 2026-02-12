#!/usr/bin/env python3
"""
WorkID 자동 생성 스크립트
새로운 WorkID를 생성하여 WORK_IN_PROGRESS.md에 추가합니다.
"""

import re
from datetime import datetime
from pathlib import Path

def get_last_workid(work_in_progress_path: str) -> tuple[str, int]:
    """
    WORK_IN_PROGRESS.md에서 마지막 WorkID를 추출합니다.

    Returns:
        tuple: (date_str, num)
    """
    try:
        with open(work_in_progress_path, 'r', encoding='utf-8') as f:
            content = f.read()

        # WIP-YYYYMMDD-NNN 형식 찾기
        pattern = r'WIP-(\d{8})-(\d{3})'
        matches = re.findall(pattern, content)

        if not matches:
            return datetime.now().strftime("%Y%m%d"), 0

        # 가장 최신 WorkID 찾기 (날짜 기준)
        latest_match = max(matches, key=lambda x: (x[0], x[1]))
        return latest_match[0], int(latest_match[1])
    except FileNotFoundError:
        return datetime.now().strftime("%Y%m%d"), 0
    except Exception as e:
        print(f"Error reading WORK_IN_PROGRESS.md: {e}")
        return datetime.now().strftime("%Y%m%d"), 0


def generate_workid(work_in_progress_path: str) -> str:
    """
    새로운 WorkID를 생성합니다.

    Args:
        work_in_progress_path: WORK_IN_PROGRESS.md 파일 경로

    Returns:
        str: 새로운 WorkID (예: WIP-20250202-001)
    """
    today = datetime.now().strftime("%Y%m%d")
    last_date, last_num = get_last_workid(work_in_progress_path)

    if last_date == today:
        # 같은 날짜면 숫자 증가
        new_num = last_num + 1
    else:
        # 다른 날짜면 1부터 시작
        new_num = 1

    return f"WIP-{today}-{new_num:03d}"


def main():
    """메인 함수"""
    # 현재 디렉토리에서 WORK_IN_PROGRESS.md 찾기
    work_in_progress_path = Path(__file__).parent.parent.parent / "WORK_IN_PROGRESS.md"

    if not work_in_progress_path.exists():
        print(f"Error: WORK_IN_PROGRESS.md not found at {work_in_progress_path}")
        return 1

    # 새로운 WorkID 생성
    new_workid = generate_workid(str(work_in_progress_path))

    print(f"✅ New WorkID: {new_workid}")
    print(f"📝 Location: {work_in_progress_path}")
    print(f"📅 Date: {new_workid.split('-')[1]}")
    print(f"🔢 Number: {new_workid.split('-')[2]}")

    # WORK_IN_PROGRESS.md에 추가 (선택 사항)
    print("\n📌 다음 명령으로 WORK_IN_PROGRESS.md를 업데이트하세요:")
    print(f"에이전트: 'WIP-XXXXXX-XXX 생성하고 WORK_IN_PROGRESS.md에 추가해줘'")

    return 0


if __name__ == "__main__":
    exit(main())
