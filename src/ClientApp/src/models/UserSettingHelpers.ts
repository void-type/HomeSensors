const settingKeyEnableFahrenheit = 'enableFahrenheit';
const settingKeyEnableHumidity = 'enableHumidity';

export default class UserSettingHelpers {
  static getInitialFahrenheitSetting() {
    const appPreference = localStorage.getItem(settingKeyEnableFahrenheit);

    // Default enabled.
    return appPreference !== 'false';
  }

  static setFahrenheit(setting: boolean) {
    localStorage.setItem(settingKeyEnableFahrenheit, setting.toString());
  }

  static getInitialHumiditySetting() {
    const appPreference = localStorage.getItem(settingKeyEnableHumidity);

    // Default enabled.
    return appPreference !== 'false';
  }

  static setHumidity(setting: boolean) {
    localStorage.setItem(settingKeyEnableHumidity, setting.toString());
  }
}
