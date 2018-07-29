using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// 빈 페이지 항목 템플릿에 대한 설명은 https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x412에 나와 있습니다.

namespace LGLight
{
    /// <summary>
    /// 자체적으로 사용하거나 프레임 내에서 탐색할 수 있는 빈 페이지입니다.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            slider.Minimum = 0;
            slider.Maximum = 100;
            slider.Value = 100;

            textBlockDeviceState.Text = "연결상태: 연결안됨";

            StartScanDevice();
        }

        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        BluetoothLEDevice device = null;
        GattDeviceService service = null;
        GattCharacteristic characteristic = null;

        private void button_Click(object sender, RoutedEventArgs e)
        {
            SetBrightnessAsync((int)slider.Value);
        }

        private void StartScanDevice()
        {
            DeviceWatcher watcher = DeviceInformation.CreateWatcher(BluetoothLEDevice.GetDeviceSelectorFromPairingState(false));
            watcher.Added += async (watcher2, deviceInfo) =>
            {
                Debug.WriteLine(deviceInfo.Id + "," + deviceInfo.Name);
                if ((deviceInfo.Name != null && deviceInfo.Name.Trim() == "LG Lighting") || (deviceInfo.Id == (string)localSettings.Values["DeviceId"]))
                {
                    if (this.device == null)
                    {
                        await ConnectAsync(deviceInfo);
                    }
                }
            };
            watcher.Removed += (watcher2, update) =>
            {
                if (this.device != null && this.device.DeviceId == update.Id)
                {
                    this.device = null;
                }
            };
            watcher.Updated += (watcher2, update) => { };
            watcher.Start();
        }

        private async Task SetBrightnessAsync(int value)
        {
            if (this.characteristic == null)
            {
                await new MessageDialog("장치 연결 안됨 혹은 사용 불가").ShowAsync();
                return;
            }

            var data = new byte[19];
            data[0] = 2;
            data[1] = 1;
            data[2] = 8;
            data[5] = (byte)(value);
            data[18] = 3;
            await this.characteristic.WriteValueAsync(data.AsBuffer());
        }

        private async Task<GattDeviceService> GetGattServiceAsync(BluetoothLEDevice device)
        {
            var services = await this.device.GetGattServicesForUuidAsync(Guid.Parse("00001851-0000-1000-8000-00805f9b34fb"));
            foreach (var service in services.Services)
            {
                return service;
            }
            return null;
        }

        private async Task<GattCharacteristic> GetGattCharacteristicAsync(GattDeviceService service)
        {
            var characteristics = await service.GetCharacteristicsForUuidAsync(Guid.Parse("0000b1e7-0000-1000-8000-00805f9b34fb"));
            foreach (var characteristic in characteristics.Characteristics)
            {
                return characteristic;
            }
            return null;
        }

        private async Task UpdateDeviceStateAsync(string state)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                textBlockDeviceState.Text = state;
            });
        }

        private async Task ConnectAsync(DeviceInformation devInfo)
        {
            this.device = await BluetoothLEDevice.FromIdAsync(devInfo.Id);
            localSettings.Values["DeviceId"] = this.device.DeviceId;

            await UpdateDeviceStateAsync("연결상태: 장치연결");
            if (this.service == null)
            {
                this.service = await GetGattServiceAsync(this.device);
                if (this.service == null)
                {
                    await UpdateDeviceStateAsync("연결상태: 장치연결. 서비스 없음");
                    return;
                }
            }
            if (this.characteristic == null)
            {
                this.characteristic = await GetGattCharacteristicAsync(this.service);
                if (this.characteristic == null)
                {
                    await UpdateDeviceStateAsync("연결상태: 장치연결. 특성 없음");
                    return;
                }
            }
            await UpdateDeviceStateAsync("연결상태: 장치연결. 사용가능");
        }
    }
}