import { inject } from 'aurelia-framework';
import { OsApiClient } from 'services/os-api-client';

let _endpoint = 'api/bets/';

@inject(OsApiClient)
export class BetsManager {

  constructor(apiClient) {
    this._apiClient = apiClient;
  }

  getBets() {
    let _url = _endpoint;

    return this._apiClient
      .get({
        url: _url
      })
      .then(response => response.json());
  }

  bet(item, up, tokenAddress){
    let _url = _endpoint;

    return this._apiClient
      .post({
        url: _url,
        data: {
          item, up, tokenAddress
        }
      })
      .then(response => response.json());
  }

  getLeaderBoard() {
    let _url = _endpoint;

    return this._apiClient
      .get({
        url: _url + 'leaderboard/'
      })
      .then(response => response.json());
  }

  deleteBet(betId){
    let _url = _endpoint;

    return this._apiClient
      .delete({
        url: _url + betId
      });
  }

}
