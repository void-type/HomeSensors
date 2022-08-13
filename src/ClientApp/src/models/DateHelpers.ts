import moment from 'moment';

function getFormattedMoment(value: moment.MomentInput, formatString: string) {
  const instant = moment(value);

  if (instant.isValid() === false) {
    return null;
  }

  return instant.format(formatString);
}

const formatStrings = {
  apiDate: 'YYYY-MM-DD',
  apiDateTime: 'YYYY-MM-DDTHH:mm:ss',
  viewDate: 'YYYY-MM-DD',
  viewDateTime: 'YYYY-MM-DD HH:mm:ss',
  viewDateTimeShort: 'YYYY-MM-DD HH:mm',
};

export default class DateHelpers {
  static dateForApi(value: moment.MomentInput) {
    return getFormattedMoment(value, formatStrings.apiDate);
  }

  static dateTimeForApi(value: moment.MomentInput) {
    return getFormattedMoment(value, formatStrings.apiDateTime);
  }

  static dateForView(value: moment.MomentInput) {
    return getFormattedMoment(value, formatStrings.viewDate);
  }

  static dateTimeForView(value: moment.MomentInput) {
    return getFormattedMoment(value, formatStrings.viewDateTime);
  }

  static dateTimeShortForView(value: moment.MomentInput) {
    return getFormattedMoment(value, formatStrings.viewDateTimeShort);
  }

  static formatStrings = formatStrings;
}
