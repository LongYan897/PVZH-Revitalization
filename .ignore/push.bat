@echo off
chcp 65001 >nul
cd /d "F:\godot\PVZH by 龙彦\pvzh-by-龙彦"
echo 正在提交...
git add .
git commit -m "更新 %date% %time%"
git push
echo.
echo ✅ 提交完成！
pause