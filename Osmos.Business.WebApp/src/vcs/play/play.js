import { inject, bindable } from 'aurelia-framework';
import { EventAggregator } from 'aurelia-event-aggregator';
import { GameDataManager } from 'managers/game-data-manager';
import { BetsManager } from 'managers/bets-manager';
import { ToastsHandler } from 'services/os-toasts-handler';
import { IdentityManager } from 'managers/identity-manager';
import anime from 'animejs/lib/anime.es.js';
import Noty from 'noty';
import Highcharts from 'highcharts';

@inject(EventAggregator, GameDataManager, BetsManager, ToastsHandler, IdentityManager)
export class Play {

  @bindable gameDataList = [];
  @bindable bets = [];
  @bindable score = 0;
  constructor(eventAggregator, gameDataManager, betsManager, toastsHandler, identityManager) {
    this._eventAggregator = eventAggregator;
    this._gameDataManager = gameDataManager;
    this._betsManager = betsManager;
    this._toastsHandler = toastsHandler;
    this._identityManager = identityManager;

    this.loading = false;

    this.averageComputeTime = 0;
  }

  async attached() {
    this.gameDataChanged = this._eventAggregator.subscribe('game-data-changed', async gameData => {
      this.startTimer();

      await this.getLatest();
      await this.getBets();
      this.notify();
    });

    this.userInfo = this._identityManager.userInfo;
    this.userInfoChanged = this._eventAggregator.subscribe('userinfo-changed', async () => {
      this.userInfo = this._identityManager.userInfo;
      await this.getBets();
    });

    this.startTimer();
    await this.getLatest();
    await this.getBets();
    this.notify();
  }

  detached() {
    this.gameDataChanged.dispose();
    this.userInfoChanged.dispose();
  }

  gameDataListChanged() {
    this.setCellCurrentBet();

    this.updateAllCharts();
  }

  updateAllCharts(){
    if (!this.charts) {
      this.charts = {
        computeTime: this.makeChart('compute-time-chart', 'compute time', 'rgb(0, 255, 255)'),
        txnsCount: this.makeChart('txns-count-chart', 'txns count', 'rgb(106, 90, 205)'),
        gasFees: this.makeChart('gas-fees-chart', 'gas fees', 'rgb(255, 127, 80)'),
        gasUsed: this.makeChart('gas-used-chart', 'gas used', 'rgb(218, 112, 214)'),
        size: this.makeChart('size-chart', 'block size', 'rgb(200, 162, 200)'),
        ethPrice: this.makeChart('eth-price-chart', 'eth price', 'rgb(230, 230, 250)')
      };
    }

    for (let index = 0; index < 12; index++) {
      if (this.charts[`token-${index}`]) continue;
      this.charts[`token-${index}`] = this.makeChart(`token-${index}-chart`, 'price', 'rgb(218, 165, 32)');
    }

    const blocks = this.gameDataList
      .filter((_, index) => index > 2)
      .reverse()
      .map(_ => _.id);

    this.updateChart('computeTime', blocks);
    this.updateChart('txnsCount', blocks);
    this.updateChart('gasFees', blocks);
    this.updateChart('gasUsed', blocks);
    this.updateChart('size', blocks);
    this.updateChart('ethPrice', blocks);

    for (let index = 0; index < 12; index++) {
      const values = this.gameDataList
        .filter((_, i) => i > 2)
        .reverse()
        .map(_ => parseFloat(_.hotTokens[index].priceUsd));

      const data = this.gameDataList
        .filter((_, index) => index > 2)
        .reverse()
        .map(_ => {
          const y = parseFloat(parseFloat(_.hotTokens[index].priceUsd));
          let symbol = 'circle';
          let fillColor = null;
          let lineWidth = null;
          let lineColor = null;
          let radius = 3;
          if (_.hotTokens[index].blocks) {
            const block = _.hotTokens[index].blocks.find(b => b.number == _.id);
            if (block.betStates && block.betStates.token && block.betStates.token.currentBet) {
              radius = 12;
              symbol = block.betStates.token.currentBet.up ? 'triangle' : 'triangle-down';
              if (block.betStates.token.currentBet.pending) fillColor = 'grey';
              else if (block.betStates.token.currentBet.success) {
                fillColor = 'green';
                lineWidth = 2;
                lineColor = block.betStates.token.currentBet.soloSuccess ? 'green' : 'red';
              }
              else {
                fillColor = 'grey';
                lineWidth = 2;
                lineColor = block.betStates.token.currentBet.soloSuccess ? 'green' : 'red';
              }
            }
          }
          return { y, marker: { symbol, fillColor, radius, lineWidth, lineColor } };
        });

      const minValue = Math.min(...data.map(_ => _.y));
      const maxValue = Math.max(...data.map(_ => _.y));

      const chart = this.charts[`token-${index}`];
      chart.xAxis[0].setCategories(blocks);
      chart.series[0].update({ data: data });
      chart.yAxis[0].setExtremes(minValue, maxValue);
      // setTimeout(() => {
      //   chart.xAxis[0].setCategories(blocks);
      //   chart.series[0].update({ data: values });
      //   chart.yAxis[0].setExtremes(minValue, maxValue);
      // }, 100);
    }
  }

  updateChart(property, blocks) {
    const data = this.gameDataList
        .filter((_, index) => index > 2)
        .reverse()
        .map(_ => {
          const y = parseFloat(_[property]);
          let symbol = 'circle';
          let fillColor = null;
          let lineWidth = null;
          let lineColor = null;
          let radius = 3;
          if (_.betStates && _.betStates[property] && _.betStates[property].currentBet) {
            radius = 9;
            symbol = _.betStates[property].currentBet.up ? 'triangle' : 'triangle-down';
            if (_.betStates[property].currentBet.pending) fillColor = 'rgb(211, 211, 211)';
            else if (_.betStates[property].currentBet.success) {
              fillColor = 'green';
              lineWidth = 2;
              lineColor = _.betStates[property].currentBet.soloSuccess ? 'green' : 'red';
            }
            else {
              fillColor = 'rgb(211, 211, 211)';
              lineWidth = 2;
              lineColor = _.betStates[property].currentBet.soloSuccess ? 'green' : 'red';
            }
          }
          return { y, marker: { symbol, fillColor, radius, lineWidth, lineColor } };
        });

      const minValue = Math.min(...data.map(_ => _.y));
      const maxValue = Math.max(...data.map(_ => _.y));

      this.charts[property].xAxis[0].setCategories(blocks);
      this.charts[property].yAxis[0].setExtremes(minValue, maxValue);
      this.charts[property].series[0].update({ data: data });
  }

  makeChart(container, name, color) {

    const options = {
      chart: {
        type: 'line',
        backgroundColor: 'rgb(13, 12, 34)',
        maxHeight: 120
      },
      title: {
        text: null
      },
      xAxis: {
        categories: [],
        labels: {
          style: {
            color: 'white'
          },
          enabled: false
        }
      },
      yAxis: {
        title: {
          text: null
        },
        labels: {
          style: {
            color: 'white'
          }
        }
      },
      plotOptions: {
        series: {
          animation: true
        }
      },
      legend: {
        enabled: false
      },
      credits: {
        enabled: false
      },
      series: [{
        name: name ?? 'value',
        data: [],
        marker: {
          symbol: 'square'
        },
        color: color ?? 'yellow',
        lineWidth: 2
      }]
    };

    return Highcharts.chart(container, options);
  }

  betsChanged() {
    this.setCellCurrentBet();
    this.averageComputeTime = this.gameDataList.filter(_ => _.computeTime).reduce((total, _) => total + _.computeTime, 0) / this.gameDataList.length;

    this.updateAllCharts();
  }

  scoreChanged(newVal, oldVal) {
    // if(newVal <= oldVal) return;
    // anime({
    //   targets: '#score',
    //   scale: [
    //     // { value: 1, duration: 0 },
    //     { value: 3, duration: 50 },
    //     { value: 1, duration: 50 }
    //   ],
    //   easing: 'easeInOutSine',
    // });
  }

  startTimer() {
    if (this.timerInterval) clearInterval(this.timerInterval);
    this.timer = 0;
    this.timerInterval = setInterval(() => {
      this.timer++;
    }, 1000);
  }

  notify() {
    const latestBlock = this.gameDataList[3].id;
    const successBets = this.bets.filter(_ => _.targetBlock == latestBlock && _.success);
    if (!successBets.length) return;
    switch (successBets.length) {
      case 1:
        this.showNotification(`<p>got <strong style="text-transform: uppercase;">x${successBets.length}</strong> multiplier</p><p>prediction level: <strong style="font-size: larger;">🦆DUCK🦆</strong></p>`, 'success', 'sunset');
        break;
      case 2:
        this.showNotification(`<p>got <strong style="text-transform: uppercase;">x${successBets.length}</strong> multiplier</p><p>prediction level: <strong style="font-size: larger;">🦢SWAN🦢</strong></p>`, 'info', 'sunset');
        break;
      case 3:
        this.showNotification(`<p>got <strong style="text-transform: uppercase;">x${successBets.length}</strong> multiplier</p><p>prediction level: <strong style="font-size: larger;">🦅EAGLE🦅</strong></p>`, 'alert', 'sunset');
        break;
      case 4:
        this.showNotification(`<p>got <strong style="text-transform: uppercase;">x${successBets.length}</strong> multiplier</p><p>prediction level: <strong style="font-size: larger;">🦉OWL🦉</strong></p>`, 'warning', 'sunset');
        break;
      default:
        this.showNotification(`<p>got <strong style="text-transform: uppercase;">x${successBets.length}</strong> multiplier</p><p>prediction level: <strong style="font-size: larger;">🔥🐦PHOENIX🐦🔥</strong></p>`, 'error', 'semanticui');
        break;
    }

    setTimeout(() => {
      anime({
        targets: '.latest-success',
        scale: [
          { value: 1, duration: 0 },
          { value: 3, duration: 50 },
          { value: 1, duration: 50 }
        ],
        easing: 'easeInOutSine',
        complete: () => {
          anime({
            targets: '.latest-success',
            translateX: [
              { value: -25, duration: 50, easing: 'easeInOutSine' },
              { value: 25, duration: 50, easing: 'easeInOutSine' },
              { value: -25, duration: 50, easing: 'easeInOutSine' },
              { value: 25, duration: 50, easing: 'easeInOutSine' },
              { value: -25, duration: 50, easing: 'easeInOutSine' },
              { value: 0, duration: 50, easing: 'easeInOutSine' }
            ],
            loop: successBets.length
          });
        }
      });

    }, 500);
  }

  getLatest() {
    return this._gameDataManager.getLatest()
      .then(gameDataList => {
        const lastBlockNumber = parseInt(gameDataList[0].id);

        const hotTokens = gameDataList[0].hotTokens;
        for (let i = 0; i < hotTokens.length; i++) {
          hotTokens[i].blocks = [];
          for (let j = 0; j < gameDataList.length; j++) {
            const hotToken = gameDataList[j].hotTokens.find(_ => _.address == hotTokens[i].address);
            hotTokens[i].blocks.push({
              number: gameDataList[j].id,
              priceUsd: hotToken ? hotToken.priceUsd : 0
            });
          }

          hotTokens[i].blocks.unshift({
            number: lastBlockNumber + 1,
            future: true
          });

          hotTokens[i].blocks.unshift({
            number: lastBlockNumber + 2,
            future: true
          });

          hotTokens[i].blocks.unshift({
            number: lastBlockNumber + 3,
            bet: true
          });
        }

        for (let i = 0; i < hotTokens.length; i++) {
          for (let j = 3; j < hotTokens[i].blocks.length - 3; j++) {
            hotTokens[i].blocks[j].priceChange = Math.sign(hotTokens[i].blocks[j].priceUsd - hotTokens[i].blocks[j + 3].priceUsd);
          }
        }

        this.hotTokens = hotTokens;

        const reversed = gameDataList.slice().reverse();
        for (let index = 3; index < reversed.length; index++) {
          reversed[index].changes = {
            txnsCount: Math.sign(reversed[index].txnsCount - reversed[index - 3].txnsCount),
            gasFees: Math.sign(reversed[index].gasFees - reversed[index - 3].gasFees),
            gasUsed: Math.sign(reversed[index].gasUsed - reversed[index - 3].gasUsed),
            size: Math.sign(reversed[index].size - reversed[index - 3].size),
            computeTime: Math.sign(reversed[index].computeTime - reversed[index - 3].computeTime),
            ethPrice: Math.sign(reversed[index].ethPrice - reversed[index - 3].ethPrice)
          };
        }
        gameDataList.unshift({
          id: lastBlockNumber + 1,
          future: true
        });
        gameDataList.unshift({
          id: lastBlockNumber + 2,
          future: true
        });
        gameDataList.unshift({
          id: lastBlockNumber + 3,
          bet: true
        });
        this.gameDataList = gameDataList;
      })
      .catch(error => {
        console.error('getLatest', error);
      });
  }

  async bet(event, item, up, tokenAddress) {
    if (this.loading) return Promise.resolve();

    if (!this.userInfo) {
      const result = await this.openConnect();
      if (!result) return;
    }

    this.loading = true;
    return this._betsManager.bet(item, up, tokenAddress)
      .then(async bet => {
        // await this.getBets();
        this.loading = false;
        this.bets.unshift(bet);
        this.betsChanged();

        let button = event.target;
        let td = button.closest('td');
        anime({
          targets: td,
          scale: [
            { value: 1, duration: 0 },
            { value: 3, duration: 30 },
            { value: 1, duration: 30 }
          ],
          easing: 'easeInOutSine',
          complete: () => {
            anime({
              targets: td,
              translateX: ['-5px', '5px', '-5px'],
              duration: 25,
              easing: 'easeInOutSine',
              direction: 'alternate',
              loop: 2
            });
          }
        });
      })
      .catch(error => {
        this.loading = false;
        console.error('bet', error);
      });
  }

  getBets() {
    return this._betsManager.getBets()
      .then(result => {
        this.bets = result.bets;
        this.score = result.score;

        this.getLeaderBoard();
      })
      .catch(error => {
        console.error('getBets', error);
      });
  }

  getLeaderBoard() {
    return this._betsManager.getLeaderBoard()
      .then(leaderboard => {
        this.leaderboard = leaderboard;
      })
      .catch(error => {
        console.error('getLeaderBoard', error);
      });
  }

  setCellCurrentBet() {
    const latestBlock = this.gameDataList[3].id;
    const blocks = this.gameDataList.map(_ => _.id.toString());
    this.currentBets = this.bets.filter(_ => blocks.includes(_.targetBlock));
    for (let i = 0; i < this.currentBets.length; i++) {
      const currentBet = this.currentBets[i];
      if (currentBet.item != 'token') {
        for (let j = 0; j < this.gameDataList.length; j++) {
          if (!this.gameDataList[j].betStates) this.gameDataList[j].betStates = {};
          if (this.gameDataList[j].id != currentBet.targetBlock) continue;
          this.gameDataList[j].betStates[currentBet.item] = {
            currentBet, class: currentBet.pending ? 'pending-bet' : currentBet.success ? 'success-bet' : 'fail-bet'
          };
          if (this.gameDataList[j].id == latestBlock && currentBet.success) {
            this.gameDataList[j].betStates[currentBet.item].class += ' latest-success';
          }
          break;
        }
      }
      else {
        for (let j = 0; j < this.hotTokens.length; j++) {
          if (this.hotTokens[j].address != currentBet.hotToken.address) continue;
          for (let k = 0; k < this.hotTokens[j].blocks.length; k++) {
            if (!this.hotTokens[j].blocks[k].betStates) this.hotTokens[j].blocks[k].betStates = {};
            if (this.hotTokens[j].blocks[k].number != currentBet.targetBlock) continue;
            this.hotTokens[j].blocks[k].betStates[currentBet.item] = {
              currentBet, class: currentBet.pending ? 'pending-bet' : currentBet.success ? 'success-bet' : 'fail-bet'
            };
            if (this.hotTokens[j].blocks[k].number == latestBlock && currentBet.success) {
              this.hotTokens[j].blocks[k].betStates[currentBet.item].class += ' latest-success';
            }
          }
        }
      }
    }
  }

  showNotification(text, type, theme) {
    new Noty({
      text: text ?? '<p>got <strong>x1</strong> multiplier</p><p>prediction level: <strong style="font-size: larger;">🦆DUCK🦆</strong></p>',
      type: type ?? 'info',
      theme: theme ?? 'nest',
      // theme: 'sunset',
      // theme: 'semanticui',
      // theme: 'metroui',
      layout: 'center',
      timeout: 2000
    }).show();
  }

  deleteBet(betState) {
    const bet = Object.assign({}, betState.currentBet);
    const betId = bet.id;
    betState.currentBet = null;
    betState.class = null;
    this.loading = true;
    return this._betsManager.deleteBet(betId)
      .then(() => {
        this.loading = false;
        this.bets = this.bets.filter(_ => _.id != betId);
      })
      .catch(error => {
        this.loading = false;
        betState.currentBet = bet;
        betState.class = 'pending-bet';
        console.error('deleteBet', error);
      });
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

}
