export class ShortAddressFormatValueConverter {
  toView(value) {
    if (!value) return null;
    // return `${value.substring(0, 4)}...${value.slice(-4)}`;
    return `${value.substring(2, 3)}..${value.slice(-2)}`;
  }
}
