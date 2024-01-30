cd poli-server
git pull origin main
cd ..
sudo ./build.sh
sudo systemctl stop poli-server
sudo systemctl start poli-server