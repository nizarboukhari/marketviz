export class SmallPriceFormatValueConverter {
  toView(value) {
    if(!value) return value;
    const num = Number(value);
    if(num < 1e-6 || num > 1e6) return num.toExponential();
    return value;
  }
}
