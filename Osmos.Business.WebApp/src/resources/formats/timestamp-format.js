import moment from 'moment';

export class TimestampFormatValueConverter {
  toView(timestamp) {
    if (!timestamp) return null;
    let m = moment(new Date(timestamp));
    if (!m.isValid()) return null;
    return m.format('DD/MM/YYYY HH:mm:ss');
  }
}
