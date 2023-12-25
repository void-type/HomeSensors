# This script helps you install mosquitto, rtl_433, and zigbee2mqtt.

# Build and install rtl_433
sudo apt-get update
sudo apt install -y libtool libusb-1.0-0-dev librtlsdr-dev rtl-sdr build-essential autoconf cmake pkg-config doxygen
cd ~
git clone https://github.com/merbanan/rtl_433.git
cd rtl_433
mkdir build
cd build
cmake ../
make
sudo make install

# Blacklist linux from taking SDR device on boot
echo -e "blacklist dvb_usb_rtl28xxu\nblacklist rtl2832\nblacklist rtl2830\nblacklist rtl2838\n" | sudo tee -a /etc/modprobe.d/raspi-blacklist.conf

# Put the user in the plugdev group so they don't need sudo to access it (just convenient for testing)
sudo usermod -a -G plugdev ${USER}

# Test rtl_433
rtl_433 -C si

# Setup MQTT server service
sudo apt install -y mosquitto mosquitto-clients

# Create new passwd file to manage mosquitto users. Enter password for clients to use with mqtt_user, put this password in the service file on line 6.
sudo mosquitto_passwd -c /etc/mosquitto/password_file rtl_433
echo -e '\npassword_file /etc/mosquitto/password_file\nlistener 1883\n' | sudo tee -a /etc/mosquitto/mosquitto.conf

# Setup other users.
sudo mosquitto_passwd /etc/mosquitto/password_file zigbee2mqtt
sudo mosquitto_passwd /etc/mosquitto/password_file homesensors_service
sudo mosquitto_passwd /etc/mosquitto/password_file homesensors_service_dev
sudo mosquitto_passwd /etc/mosquitto/password_file homesensors_web
sudo mosquitto_passwd /etc/mosquitto/password_file homesensors_web_dev

# I also turn persistence off since our client doesn't care.

sudo systemctl enable mosquitto
sudo systemctl status mosquitto

# Setup rtl_433 as a service
# Review and copy the service.
sudo mv ~/rtl_433.service /etc/systemd/system/rtl_433.service
sudo chmod 664 /etc/systemd/system/rtl_433.service

# Edit if needed, save to string above
# sudo vi /etc/systemd/system/rtl_433.service

# Use this after making edits
# sudo systemctl daemon-reload; sudo systemctl restart rtl_433;

sudo systemctl daemon-reload
sudo systemctl start rtl_433
sudo systemctl enable rtl_433
sudo systemctl status rtl_433

# Test. Listen for the models you own, # is wild card. Our service sets topics up like rtl_433/model/id
# mosquitto_sub -h localhost -t "rtl_433/Acurite-986/#" -t "rtl_433/Acurite-609TXC/149" -u homesensors_service -P ***PASSWORD***

# Setup zigbee2mqtt dependencies (runs on nodejs)
sudo apt-get update
sudo apt-get install -y ca-certificates curl gnupg
sudo mkdir -p /etc/apt/keyrings
curl -fsSL https://deb.nodesource.com/gpgkey/nodesource-repo.gpg.key | sudo gpg --dearmor -o /etc/apt/keyrings/nodesource.gpg
NODE_MAJOR=20
echo "deb [signed-by=/etc/apt/keyrings/nodesource.gpg] https://deb.nodesource.com/node_$NODE_MAJOR.x nodistro main" | sudo tee /etc/apt/sources.list.d/nodesource.list
sudo apt-get update
sudo apt-get install nodejs -y

# Clone and build zigbee2mqtt
sudo mkdir /opt/zigbee2mqtt
sudo chown -R ${USER}: /opt/zigbee2mqtt

# Clone Zigbee2MQTT repository
git clone --depth 1 https://github.com/Koenkk/zigbee2mqtt.git /opt/zigbee2mqtt
cd /opt/zigbee2mqtt
npm ci
npm run build

# Review and copy the zigbee2mqtt configuration. You will need your MQTT credentials for the zigbee2mqtt user.
mv ~/zigbee2mqtt.configuration.yaml /opt/zigbee2mqtt/data/configuration.yaml

# Review and copy the zigbee2mqtt service.
sudo mv ~/zigbee2mqtt.service /etc/systemd/system/zigbee2mqtt.service
sudo chmod 664 /etc/systemd/system/zigbee2mqtt.service

# Test app
cd /opt/zigbee2mqtt
npm start

# Test and enable service
sudo systemctl daemon-reload
sudo systemctl start zigbee2mqtt
sudo systemctl enable zigbee2mqtt
sudo systemctl status zigbee2mqtt

# Tip: see the logs of the services:
sudo journalctl -u zigbee2mqtt -f
sudo journalctl -u rtl_433 -f
sudo journalctl -u mosquitto -f

# Use the web frontend to make future zigbee2mqtt configuration changes, including pairing new devices, at http://localhost:8080
