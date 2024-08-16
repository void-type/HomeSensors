import { format, formatISO, formatISO9075 } from 'date-fns';
import { isNil } from './FormatHelpers';

const formatStrings = {
  // apiDate: 'yyyy-MM-dd',
  // apiDateTime: "yyyy-MM-dd'T'HH:mm:ss",
  // viewDate: 'yyyy-MM-dd',
  // viewDateTime: 'yyyy-MM-dd HH:mm:ss',
  viewDateTimeShort: 'yyyy-MM-dd HH:mm',
};

export default class DateHelpers {
  static dateForApi(value: Date) {
    return formatISO(value as Date, { representation: 'date' });
  }

  static dateTimeForApi(value: Date) {
    return formatISO(value);
  }

  static dateForView(value: Date) {
    return formatISO9075(value as Date, { representation: 'date' });
  }

  static dateTimeForView(value: Date) {
    return formatISO9075(value as Date);
  }

  static dateTimeShortForView(value: Date | string | undefined): string {
    if (typeof value === 'undefined') {
      return '';
    }

    if (typeof value === 'string') {
      if (isNil(value)) {
        return '';
      }

      return this.dateTimeShortForView(new Date(value));
    }

    return format(value, formatStrings.viewDateTimeShort);
  }
}
