# Build and install rtl_433
cd ~

sudo apt-get update
sudo apt install -y libtool libusb-1.0-0-dev librtlsdr-dev rtl-sdr build-essential autoconf cmake pkg-config doxygen

git clone https://github.com/merbanan/rtl_433.git

cd rtl_433
mkdir build
cd build
cmake ../
make
sudo make install

# Blacklist linux from taking device on boot
echo -e "blacklist dvb_usb_rtl28xxu\nblacklist rtl2832\nblacklist rtl2830\nblacklist rtl2838\n" | sudo tee -a /etc/modprobe.d/raspi-blacklist.conf

# Put the user in the plugdev group so they don't need sudo to access it (just convenient for testing)
sudo usermod -a -G plugdev pi

# Test rtl_433
rtl_433 -C si

# Setup MQTT server service
sudo apt install -y mosquitto mosquitto-clients

# Create new passwd file. Enter password for clients to use with mqtt_user, put this password in the service file on line 6.
sudo mosquitto_passwd -c /etc/mosquitto/password_file mqtt_user
echo -e '\npassword_file /etc/mosquitto/password_file\n' | sudo tee -a /etc/mosquitto/mosquitto.conf

sudo systemctl enable mosquitto
sudo systemctl status mosquitto

# Setup rtl_433 as a service
sudo touch /etc/systemd/system/rtl_433.service
sudo chmod 664 /etc/systemd/system/rtl_433.service

# Review and edit the rtl_433.service file. You'll need to add the password you set above on line 6.
# Use the following commented command to copy the service file to a string and replace the string in the next command.
# (Get-Content -Path .\rtl_433.service -Raw) -replace "`n", "\n" | clip
echo -e '*** SERVICE FILE CONTENT ***' | sudo tee /etc/systemd/system/rtl_433.service

# Edit if needed, save to string above
# sudo vi /etc/systemd/system/rtl_433.service

# Use this after making edits
# sudo systemctl daemon-reload; sudo systemctl restart rtl_433;

sudo systemctl daemon-reload
sudo systemctl start rtl_433
sudo systemctl enable rtl_433
sudo systemctl status rtl_433

# Test. Listen for the models you own, # is wild card. Our service sets topics up like rtl_433/model/id
# mosquitto_sub -h localhost -t "rtl_433/Acurite-986/#" -t "rtl_433/Acurite-609TXC/149" -u mqtt_user -P ***PASSWORD***
