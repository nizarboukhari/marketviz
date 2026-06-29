
export class OsGuid {

  static new() {
    let d = new Date().getTime();
    let d2 = ((typeof performance !== 'undefined') && performance.now && (performance.now() * 1000)) || 0;
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
      let r = Math.random() * 16;
      if (d > 0) {
        r = (d + r) % 16 | 0;
        d = Math.floor(d / 16);
      } else {
        r = (d2 + r) % 16 | 0;
        d2 = Math.floor(d2 / 16);
      }
      return (c === 'x' ? r : (r & 0x3 | 0x8)).toString(16);
    });
  }

  // to deprecate
  static New() {
    return OsGuid._guid();
  }

  static _guid() {
    return (
      OsGuid._s4() +
      OsGuid._s4() +
      '-' +
      OsGuid._s4() +
      '-' +
      OsGuid._s4() +
      '-' +
      OsGuid._s4() +
      '-' +
      OsGuid._s4() +
      OsGuid._s4() +
      OsGuid._s4()
    );
  }

  static _s4() {
    return Math.floor((1 + Math.random()) * 0x10000)
      .toString(16)
      .substring(1);
  }
}
