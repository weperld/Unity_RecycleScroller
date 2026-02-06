# Git 리포지토리 설정 스크립트

# 1. Unity 프로젝트 파일 무시 설정
# 현재 위치의 .gitignore를 수정하여 패키지만 추적

# 2. 패키지 폴더만 추적하도록 Git 설정
git config core.sparseCheckout true
git config sparse-checkout.initial false

# 3. 패키지 폴더만 Git에 추가
git add Assets/RecycleScrollerPackage/
git add .gitignore
git add README.md

echo "Git 설정 완료. 패키지 폴더만 추적됩니다."
