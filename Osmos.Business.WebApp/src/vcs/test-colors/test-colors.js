import { ColorsGenerator } from 'services/colors-generator';

export class TestColors {

  constructor() {
    this.colorsGenerator = new ColorsGenerator();

    this.bgColor = [28, 27, 48];

    this.wallets = [];
  }

  addColor() {
    const pair = this.colorsGenerator.randomPair();
    console.info('pair', pair);
    
    const randomCharacters = this.generateRandomCharacters();
    this.wallets.push({
      address: randomCharacters,
      colors: pair
    });
  }

  getRandomCharacter(array){
    return array[Math.floor(Math.random() * array.length)];
  }

  generateRandomCharacters(numChars) {
    const letters = 'abcdefghijklmnopqrstuvwxyz'.split('');
    const numbers = '0123456789'.split('');
  
    let randomCharacters = [];
    for (let i = 0; i < numChars; i++) {
      const character = Math.random() < 0.5 ? this.getRandomCharacter(letters) : this.getRandomCharacter(numbers);
      randomCharacters.push(character);
    }
  
    return randomCharacters;
  }

}
