import { format, formatISO, formatISO9075 } from 'date-fns';

const formatStrings = {
  // apiDate: 'yyyy-MM-dd',
  // apiDateTime: "yyyy-MM-dd'T'HH:mm:ss",
  // viewDate: 'yyyy-MM-dd',
  // viewDateTime: 'yyyy-MM-dd HH:mm:ss',
  viewDateTimeShort: 'yyyy-MM-dd HH:mm',
};

export default class DateHelpers {
  static dateForApi(value: Date | string | null | undefined) {
    return formatISO(value as Date, { representation: 'date' });
  }

  static dateTimeForApi(value: Date | null | undefined) {
    return formatISO(value as Date);
  }

  static dateForView(value: Date | null | undefined) {
    return formatISO9075(value as Date, { representation: 'date' });
  }

  static dateTimeForView(value: Date | string | null | undefined) {
    return formatISO9075(value as Date);
  }

  static dateTimeShortForView(value: Date | string | null | undefined) {
    return format(new Date(value as string), formatStrings.viewDateTimeShort);
  }
}
