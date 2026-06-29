
import { inject } from 'aurelia-framework';
import { EventAggregator } from 'aurelia-event-aggregator';
import { DialogService } from 'aurelia-dialog';
import { ToastsHandler } from 'services/os-toasts-handler';
import { TokensManager } from 'managers/tokens-manager';
import { TxnsManager } from 'managers/txnsManager';
import { HotTokensManager } from 'managers/hot-tokens-manager';
import { DataInfosManager } from 'managers/data-infos-manager';
import { EthPriceManager } from 'managers/eth-price-manager';
import { IdentityManager } from 'managers/identity-manager';
import { PromotedTokensManager } from 'managers/promoted-tokens-manager';
import { Chain } from 'repeat';
// import tippy from 'tippy.js';
import moment from 'moment';

@inject(DialogService, EventAggregator, ToastsHandler, TokensManager, TxnsManager, HotTokensManager, DataInfosManager, EthPriceManager, IdentityManager, PromotedTokensManager)
export class Trending {

  constructor(dialogService, eventAggregator, toastsHandler, tokensManager, txnsManager, hotTokensManager, dataInfosManager, ethPriceManager, identityManager, promotedTokensManager) {
    this._dialogService = dialogService;
    this._eventAggregator = eventAggregator;
    this._toastsHandler = toastsHandler;
    this._tokensManager = tokensManager;
    this._txnsManager = txnsManager;
    this._hotTokensManager = hotTokensManager;
    this._dataInfosManager = dataInfosManager;
    this._ethPriceManager = ethPriceManager;
    this._identityManager = identityManager;
    this._promotedTokensManager = promotedTokensManager;

    this.chain = new Chain();

    this.sort = {
      desc: true,
      prop: null
    };

    this.showList = true;

  }

  attached() {

    this.userInfo = this._identityManager.userInfo;
    this.userInfoChanged = this._eventAggregator.subscribe('userinfo-changed', () => {
      this.userInfo = this._identityManager.userInfo;
      if (!this.userInfo) this.token = null;
    });

    this.chain
      .add(() => this.getHotTokens(), () => this.getDataInfos())
      .every(3000);

    // const wideDivRect = document.getElementById('wide-div').getBoundingClientRect();
    // this.hideEyeButton = wideDivRect.width < 972;

    this.ethPriceChanged = this._eventAggregator.subscribe('eth-price-changed', ethPrice => {
      this.ethPrice = ethPrice;
    });
  }

  detached() {
    this.userInfoChanged.dispose();
    this.chain.cancel();
    this.ethPriceChanged.dispose();
  }

  getHotTokens() {
    this.loading = true;
    return this._hotTokensManager.getHotTokens()
      .then(result => {

        // this.setGraphData(result);

        this.rawResult = Object.assign([], result);
        this._eventAggregator.publish('hot-tokens-changed');

        this.og = result;

        let _result = this.og.map(item => {
          if (item.scoreDates.length) {
            item.scoreDates = item.scoreDates.map((_, index, array) => {
              _.moment = moment(_.date);
              _.duration = null;
              if (index > 0) {
                _.duration = -_.moment.diff(array[array.length - 1].moment, 'seconds', true);
              }
              return _;
            });
          }

          let sub = item.scoreDates
            .filter(_ => _.duration != null)
            .filter(_ => _.duration < 180);
          item.sub = sub;
          item.lastScore = sub.length ? sub[0].score : item.score;
          item.scoreDiff = item.score - item.lastScore;

          return item;
        });

        // console.info('_result', _result);

        this.sortArray();
        this.loading = false;
      })
      .catch(error => {
        console.error('getHotTokens', error);
        this.loading = false
      });
  }

  getDataInfos() {
    if (!this.userInfo) return Promise.resolve();
    this.loading = true;
    return this._dataInfosManager.getDataInfos()
      .then(result => {
        this.dataInfos = result;
        // console.info('this.dataInfos', this.dataInfos);
      })
      .catch(error => {
        console.error('getDataInfos', error);
        this.loading = false
      });
  }

  // obsolete
  getTrending() {
    this.loading = true;
    return this._tokensManager.getTrending()
      .then(result => {

        this.og = result;

        this.sortArray();
        this.loading = false;
      })
      .catch(error => {
        console.error('getTrending', error);
        this.loading = false
      });
  }

  getTokenTxns() {
    if (!this.token || !this.token.address || !this.token.address.length == 42) return Promise.resolve();
    this.loading = true;
    return this._txnsManager.getTokenTxns(this.token.address, 48)
      .then(txns => {
        if (!this.token) return;
        this.txns = txns.filter(t => t.tokenAddress == this.token.address);
        this.loading = false
      })
      .catch(error => {
        // console.error('getTokenTxns', error);
        this.loading = false
      });
  }

  getTokensTxns() {
    let addresses = this.result.map(_ => _.token.address);
    this.loading = true;
    return this._txnsManager.getTokensTxns(addresses, 48)
      .then(txns => {
        this.txns = txns.filter(t => t.tokenAddress == this.token.address);
        this.loading = false
      })
      .catch(error => {
        // console.error('getTokenTxns', error);
        this.loading = false
      });
  }

  getOwnerTxns() {
    if (!this.ownerAddress || !this.ownerAddress == 42) return Promise.resolve();
    this.loading = true;
    this._txnsManager.getOwnerTxns(this.ownerAddress, 48)
      .then(txns => {
        this.txns = txns;
        this.loading = false
      })
      .catch(error => {
        // console.error('getOwnerTxns', error);
        this.loading = false
      });
  }

  formatCash(num) {
    const n = Math.abs(num);
    const prefix = (num < 0 ? '-' : '');
    if (n == 0) return prefix + 0;
    if (n < 0.1) return '~' + prefix + '0';
    if (n < 1e3) return prefix + n;
    if (n >= 1e3 && n < 1e6) return prefix + (n / 1e3).toFixed(1) + "K";
    if (n >= 1e6 && n < 1e9) return prefix + (n / 1e6).toFixed(1) + "M";
    if (n >= 1e9 && n < 1e12) return prefix + (n / 1e9).toFixed(1) + "B";
    if (n >= 1e12 && n < 1e14) return prefix + (n / 1e12).toFixed(1) + "T";
    const sign = num < 0 ? '<' : '>';
    return sign + prefix + '99T';
  }

  copyTokenAddress(token) {
    navigator.clipboard.writeText(token.address);

    const message = `copied token "${token.displayName}" address in the clipboard`;
    // console.info(this._toastsHandler);
    this._toastsHandler.info(message);
  }


  async watchToken(token) {

    if (!this.userInfo) {
      const result = await this.openConnect();
      if (!result) return;
    }

    if (this.token && token.address == this.token.address) {
      this.token = null;
      return;
    }
    
    this.token = token;
    window.scrollTo(0, 0);
    return;
    this._dialogService.open({
      viewModel: PLATFORM.moduleName('vcs/tokens/watch-token'), model: {
        token: this.token
      }, lock: false
    }).whenClosed(response => {
      this.token = null;
      if (response.wasCancelled) return;
    });
  }

  openSourceCodeDialog(name, sourceCodeObject) {
    this._dialogService.open({
      viewModel: PLATFORM.moduleName('resources/dialogs/source-code-display'), model: {
        name: name,
        sourceCodeObject: sourceCodeObject
      }, lock: false
    }).whenClosed(response => {
      if (response.wasCancelled) return;
    });
  }

  convertPriceToETH(wei) {
    if (!wei) return '';
    const result = wei / 1000000000000000000;
    return result.toFixed(2);
  }

  convertPriceToUSD(wei) {
    if (!wei) return '';
    const result = this._ethPriceManager.ethPrice * wei / 1000000000000000000;
    return result.toFixed(2) + '$';
  }

  setSort(prop, event) {

    event.preventDefault();

    if (this.sort.prop == prop) {
      this.sort.desc = !this.sort.desc;
    }
    else {
      this.sort.prop = prop;
      this.sort.desc = true;
    }

    this.sortArray();

  }

  resetSort() {
    this.sort = {
      desc: true,
      prop: null
    };

    this.sortArray();
  }

  sortArray() {

    const order = this.sort.desc ? 1 : -1;

    let _result = [];

    if (this.sort.prop == 'date') {
      _result = this.og.sort((a, b) => {
        if (a.token.pairTradingData.pairCreatedAt < b.token.pairTradingData.pairCreatedAt) return 1 * order;
        if (a.token.pairTradingData.pairCreatedAt > b.token.pairTradingData.pairCreatedAt) return -1 * order;
        return 0;
      });
    }
    else if (this.sort.prop == 'holders') {
      _result = this.og.sort((a, b) => {
        if (!a.token.tokenInfo && !b.token.tokenInfo) return 0;
        if (!a.token.tokenInfo) return 1 * order;
        if (!b.token.tokenInfo) return -1 * order;

        if (a.token.tokenInfo.holdersCount < b.token.tokenInfo.holdersCount) return -1 * order;
        if (a.token.tokenInfo.holdersCount > b.token.tokenInfo.holdersCount || !b.token.tokenInfo) return 1 * order;
        return 0;
      });
    }
    else if (this.sort.prop == 'mrktcap') {
      _result = this.og.sort((a, b) => {
        if (a.token.pairTradingData.fdv < b.token.pairTradingData.fdv) return -1 * order;
        if (a.token.pairTradingData.fdv > b.token.pairTradingData.fdv) return 1 * order;
        return 0;
      });
    }
    else {
      _result = this.og.sort((a, b) => {
        if (a.score < b.score) return 1 * order;
        if (a.score > b.score) return -1 * order;
        return 0;
      });
    }

    const rotationSeconds = 12;
    const now = Date();
    let promotedIndex = {
      date: now.toString(),
      index: 0
    };
    const promotedIndexString = localStorage.getItem('promoted-index');
    if(promotedIndexString){
      promotedIndex = JSON.parse(promotedIndexString);
      if((new Date() - new Date(promotedIndex.date)) / 1000 > rotationSeconds){
        promotedIndex.index++;
        if(promotedIndex.index >= this._promotedTokensManager.promotedTokens.length) promotedIndex.index = 0;
        promotedIndex.date = now.toString();

        localStorage.setItem('promoted-index', JSON.stringify(promotedIndex));
      }
    }
    else{
      localStorage.setItem('promoted-index', JSON.stringify(promotedIndex));
    }

    const promotedToken = this._promotedTokensManager.promotedTokens[promotedIndex.index];
    if(!this.userInfo && promotedToken) {

      promotedToken.promoted = true;

      const indexOfFirst = 0;
      for (let index = 1; index < _result.length; index++) {
        const element = _result[index];
        if(element.score > _result[indexOfFirst].score) indexOfFirst = index;
      }
      _result.splice(indexOfFirst, 1);
      _result.splice(1, 0, {
        score: null,
        token: promotedToken,
        promoted: true
      });
    }

    this.result = Object.assign([], _result);

    _result = [];
    _result = null;
  }

  toExpoNotation(value) {
    if (value >= 0.000001) return value;
    return Number.parseFloat(value).toExponential(2);
  }

  openDexscreener(token) {
    window.open(`https://dexscreener.com/ethereum/${token.pairAddress ? token.pairAddress : token.address}`, '_blank');
  }

  openConnect() {
    return new Promise(async (resolve, reject) => {
      if (this.userInfo) {
        console.warn('already connected');
        this._toastsHandler.success('you are now connected to MarketViz and can start enjoying our full range of trading features.');
        resolve(true);
      }

      if (window.ethereum) {
        const accounts = await window.ethereum.request({ method: 'eth_requestAccounts' });
        const address = accounts[0];
        await this._identityManager.login(address);
        if (this.userInfo) this._toastsHandler.success('you are now connected to MarketViz and can start enjoying our full range of trading features.');
        resolve(!!this.userInfo);
      } else {
        console.error('Metamask not available');
        alert('To use MarketViz, you\'ll need to authenticate with Metamask by installing it and choosing a wallet. If you\'re using an iPhone, please note that you can still install Metamask but will need to use its built-in browser instead of Safari.');
        resolve(false);
      }
      return;

      this._dialogService.open({
        viewModel: PLATFORM.moduleName('resources/dialogs/connect'), model: null, lock: true
      }).whenClosed(async response => {
        if (response.wasCancelled) {
          resolve(false);
          return;
        }

        if (window.ethereum) {
          const accounts = await window.ethereum.request({ method: 'eth_requestAccounts' });
          const address = accounts[0];
          await this._identityManager.login(address);
          if (this.userInfo) this._toastsHandler.success('you are now connected to MarketViz and can start enjoying our full range of trading features.');
          resolve(!!this.userInfo);
        } else {
          console.error('Metamask not available');
          alert('To use MarketViz, you\'ll need to authenticate with Metamask by installing it and choosing a wallet. If you\'re using an iPhone, please note that you can still install Metamask but will need to use its built-in browser instead of Safari.');
          resolve(false);
        }
      });
    });
  }

  setShowList(test) {
    this.showList = test;
  }

  getCardClass(item){
    if(item.promoted) return '';
    if(item.token.owner == '0x0000000000000000000000000000000000000000' && (item.token.sourceCode && !item.token.sourceCode.missing)) return 'card-low';
    if(item.token.owner != '0x0000000000000000000000000000000000000000' && (item.token.sourceCode && !item.token.sourceCode.missing)) return 'card-medium';
    if(item.token.owner == '0x0000000000000000000000000000000000000000' && !(item.token.sourceCode && !item.token.sourceCode.missing)) return 'card-medium';
    return 'card-high';
  }

}
