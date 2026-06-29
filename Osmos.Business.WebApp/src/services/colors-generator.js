export class ColorsGenerator {
  colorPairs = [];
  bgColor = [28, 27, 48];

  constructor(bgColor){
    if(bgColor){
      this.bgColor = bgColor;
    }
  }

  generateColorPairs() {
    const pairs = [];

    // Get the luminance value of the background color
    // const bgLuminance = this.getLuminance(this.bgColor);

    // Generate random colors and their negatives until we have 72 pairs
    while (pairs.length < 72) {
      const color = this.generateRandomColor();
      const negative = this.getNegativeColor(color);
      const contrastRatio = this.getContrastRatio(color, negative);

      // If the contrast ratio meets the minimum requirement, add the pair to the array
      if (contrastRatio >= 4.5) {
        pairs.push([color, negative]);
      }
    }

    this.colorPairs = pairs.map(pair => [this.rgbToHex(pair[0]), this.rgbToHex(pair[1])]);
    return this.colorPairs;
  }

  randomPair() {
    let pair = [];
    let found = false;
    while (!found) {
      const color0 = this.generateRandomColor();
      const color1 = this.generateRandomColor();

      const contrastRatio0 = this.getContrastRatio(color0, this.bgColor);
      const contrastRatio1 = this.getContrastRatio(color1, this.bgColor);

      if (contrastRatio0 > 4.5 && contrastRatio1 > 4.5) {
        pair = [color0, color1];
        found = true;
      }
    }
    return [this.rgbToHex(pair[0]), this.rgbToHex(pair[1])];
  }

  rgbToHex(rgb) {
    // Make sure the RGB values are within the valid range of 0-255
    const r = Math.max(0, Math.min(255, rgb[0]));
    const g = Math.max(0, Math.min(255, rgb[1]));
    const b = Math.max(0, Math.min(255, rgb[2]));

    // Convert each component to a hex string and concatenate them
    return "#" + ((1 << 24) + (r << 16) + (g << 8) + b).toString(16).slice(1);
  }

  // Helper function to generate a random RGB color
  generateRandomColor() {
    const red = Math.floor(Math.random() * 256);
    const green = Math.floor(Math.random() * 256);
    const blue = Math.floor(Math.random() * 256);
    return [red, green, blue];
  }

  // Helper function to get the negative color of an RGB color
  getNegativeColor(rgb) {
    const negative = [];
    for (let i = 0; i < 3; i++) {
      negative.push(255 - rgb[i]);
    }
    return negative;
  }

  // Helper function to calculate the relative luminance of an RGB color
  getLuminance(rgb) {
    const sRGB = [];
    for (let i = 0; i < 3; i++) {
      const sRGBComponent = rgb[i] / 255;
      sRGB.push(sRGBComponent <= 0.03928 ? sRGBComponent / 12.92 : ((sRGBComponent + 0.055) / 1.055) ** 2.4);
    }
    return 0.2126 * sRGB[0] + 0.7152 * sRGB[1] + 0.0722 * sRGB[2];
  }

  // Helper function to calculate the contrast ratio between two colors
  getContrastRatio(color1, color2) {
    const luminance1 = this.getLuminance(color1);
    const luminance2 = this.getLuminance(color2);
    const brighter = Math.max(luminance1, luminance2);
    const darker = Math.min(luminance1, luminance2);
    return (brighter + 0.05) / (darker + 0.05) > 1 ? (brighter + 0.05) / (darker + 0.05) : (darker + 0.05) / (brighter + 0.05);
  }
}
