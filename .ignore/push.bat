@echo off
chcp 65001 >nul
cd /d "F:\godot\PVZH by 龙彦\pvzh-by-龙彦"

echo ================================
echo    Git 提交助手
echo ================================
echo.
echo 当前项目: PVZH-Revitalization
echo.
echo 提示: 按 Ctrl+C 可取消提交
echo.

set /p commit_msg="请输入提交信息（按回车确认）: "

if "%commit_msg%"=="" (
    echo.
    echo ❌ 提交信息不能为空，已取消提交
    pause
    exit
)

echo.
echo 正在提交...
git add .
git commit -m "%commit_msg%"
git push

echo.
echo ✅ 提交完成！
pause