import moment from 'moment';

export class MomentFormatValueConverter{
  toView(value, options) {
    if(!value) return null;
    let m = moment.utc(value).local();
    if(!m.isValid()) return null;

    let format = 'YYYY/MM/DD HH:mm';
    if(options && options.format){
      format = options.format;
    }
    
    let result = m.format(format);
    return result;
  }
}
