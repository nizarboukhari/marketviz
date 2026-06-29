import moment from 'moment';

export class HumanizeTimestampFormatValueConverter {
  toView(timestamp) {
    if (!timestamp) return null;
    let m = moment(new Date(timestamp));
    if (!m.isValid()) return null;
    return moment.duration(m.diff(moment())).humanize(true);
  }
}
