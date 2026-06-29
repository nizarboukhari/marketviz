import { inject } from 'aurelia-framework';
import { TokensManager } from 'managers/tokens-manager';

@inject(TokensManager)
export class BestOf {

  constructor(tokensManager) {
    this._tokensManager = tokensManager;

    this.show = 'opportunity';
  }

  attached() {
    this.getOpportunity();
    this.getScore();
  }

  getOpportunity() {
    this.opportunity;
    this.loading = true;
    return this._tokensManager.getOpportunity()
      .then(opportunity => {
        this.loading = false;
        this.opportunity = opportunity;
        console.info('this.opportunity', this.opportunity);
      })
      .catch(error => {
        this.loading = false;
        console.error('getOpportunity', error);
      });
  }

  getScore() {
    this.score;
    this.loading = true;
    return this._tokensManager.getScore()
      .then(score => {
        this.loading = false;
        this.score = score;
        console.info('this.score', this.score);
      })
      .catch(error => {
        this.loading = false;
        console.error('getScore', error);
      });
  }

}
