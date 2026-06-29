import { inject, customElement, bindable, bindingMode } from 'aurelia-framework';
import { EventAggregator } from 'aurelia-event-aggregator';
import { DialogService } from 'aurelia-dialog';
import { TokensManager } from 'managers/tokens-manager';
import { TxnsManager } from 'managers/txnsManager';
import { HotTokensManager } from 'managers/hot-tokens-manager';
import { EthPriceManager } from 'managers/eth-price-manager';
import { ToastsHandler } from 'services/os-toasts-handler';
import { Chain } from 'repeat';
import { ColorsGenerator } from 'services/colors-generator';
import Highcharts from 'highcharts';
import moment from 'moment';

@customElement('token-info')
@inject(EventAggregator, DialogService, TokensManager, TxnsManager, HotTokensManager, EthPriceManager, ToastsHandler)
export class WatchToken {

  @bindable({ defaultBindingMode: bindingMode.twoWay }) token;
  @bindable item = null;

  constructor(eventAggregator, dialogService, tokensManager, txnsManager, hotTokensManager, ethPriceManager, toastsHandler) {
    this._eventAggregator = eventAggregator;
    this._dialogService = dialogService;
    this._tokensManager = tokensManager;
    this._txnsManager = txnsManager;
    this._hotTokensManager = hotTokensManager;
    this._ethPriceManager = ethPriceManager;
    this._toastsHandler = toastsHandler;

    this.chain = new Chain();

    this.colorsGenerator = new ColorsGenerator([28, 27, 48]);

    this.showCharts = false;

    this.botsRatio = {buys: 0, sells: 0};
  }

  attached() {

    this.initCharts();

    this.chain
      .add(() => this.getToken(), () => this.getTokenTxns())
      .every(3000);

    this.hotTokenChanged = this._eventAggregator.subscribe('hot-tokens-changed', () => {
      this.setItem();
    });
  }

  detached() {
    this.hotTokenChanged.dispose();
  }

  setItem() {
    this.item = null;
    if (!this.token) return;
    this.item = this._hotTokensManager.hotTokens.find(_ => _.token.address == this.token.address);
    if (!this.item) return;
    this.item.rank = this._hotTokensManager.hotTokens.map(_ => _.token.address).indexOf(this.token.address) + 1;
  }

  itemChanged() {
    this.setScoreData();
  }

  tokenChanged() {
    this.txns = [];
    this.wallets = [];
    this.setItem();
  }

  getToken() {
    if (!this.token || !this.token.address || !this.token.address.length == 42) return Promise.resolve();
    this.loading = true;
    return this._tokensManager.getToken(this.token.address)
      .then(token => {
        this.loading = false;
        this.setPieChartData(token);
        this.setPriceData(token);
        for (let prop in this.token) {
          this.token[prop] = token[prop];
        }
      })
      .catch(error => {
        this.loading = false;
        console.error('getToken', error);
      });
  }

  getTokenTxns() {
    if (!this.token || !this.token.address || !this.token.address.length == 42) return Promise.resolve();
    this.loading = true;
    return this._txnsManager.getTokenTxns(this.token.address, 48)
      .then(txns => {
        this.loading = false
        if (!this.token) return;
        this.txns = txns.filter(t => t.tokenAddress == this.token.address);
        const addresses = this.txns.map(_ => _.from).filter((value, index, array) => {
          return array.indexOf(value) === index;
        });

        let addressesToDelete = [];
        for (let index = 0; index < this.wallets.length; index++) {
          const wallet = this.wallets[index];
          const address = addresses.find(_ => _ == wallet.address);
          if (!address) addressesToDelete.push(wallet.address);
        }
        this.wallets = this.wallets.filter(wallet => addresses.find(_ => _ == wallet.address));

        for (let index = 0; index < addresses.length; index++) {
          const address = addresses[index];
          const existing = this.wallets.find(_ => _.address == address);
          if (existing) continue;
          this.wallets.push({
            address: address,
            colors: this.colorsGenerator.randomPair()
          });
        }

        for (let index = 0; index < this.txns.length; index++) {
          const wallet = this.wallets.find(_ => _.address == this.txns[index].from);
          this.txns[index].colors = wallet.colors;
        }

        const _buys = this.txns.filter(_ => _.type == 'Buy');
        const _sells = this.txns.filter(_ => _.type == 'Sell');

        this.botsRatio = {
          buys: !_buys.length ? 0 :
            _buys.filter(_ => _.functionName != 'execute' && _.functionName != 'multicall').length / _buys.length,
          sells: !_sells.length ? 0 :
            _sells.filter(_ => _.functionName != 'execute' && _.functionName != 'multicall').length / _sells.length
        };

        console.info('this.botsRatio', this.botsRatio);

      })
      .catch(error => {
        console.error('getTokenTxns', error);
        this.loading = false;
      });
  }

  convertPriceToUSD(wei) {
    if (!wei) return '';
    const result = this._ethPriceManager.ethPrice * wei / 1000000000000000000;
    return result;
  }

  unwatch() {
    this.token = null;
  }

  formatCash(num, dontFix) {
    const n = Math.abs(num);
    const prefix = (num < 0 ? '-' : '');
    if (n == 0) return prefix + 0;
    if (n < 0.1) return '~' + prefix + '0';
    if (n < 1e3) return prefix + (!dontFix ? n.toFixed(2) : n);
    if (n >= 1e3 && n < 1e6) return prefix + (n / 1e3).toFixed(1) + "K";
    if (n >= 1e6 && n < 1e9) return prefix + (n / 1e6).toFixed(1) + "M";
    if (n >= 1e9 && n < 1e12) return prefix + (n / 1e9).toFixed(1) + "B";
    if (n >= 1e12 && n < 1e14) return prefix + (n / 1e12).toFixed(1) + "T";
    const sign = num < 0 ? '<' : '>';
    return sign + prefix + '99T';
  }

  toExpoNotation(value) {
    if (value >= 0.000001) return value;
    return Number.parseFloat(value).toExponential(2);
  }

  copyTokenAddress(token) {
    navigator.clipboard.writeText(token.address);

    const message = `copied token "${token.displayName}" address in the clipboard`;
    this._toastsHandler.info(message);
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

  setShowCharts(test, event) {
    event.preventDefault();
    this.showCharts = test;
  }

  initCharts() {
    this.pieChart = Highcharts.chart('pie-chart-container', {
      chart: {
        plotBackgroundColor: null,
        plotBorderWidth: null,
        plotShadow: false,
        type: 'pie',
        backgroundColor: 'rgba(0,0,0,0)'
      },
      credits: {
        enabled: false
      },
      title: {
        text: 'Holders',
        align: 'center',
        style: {
          color: '#fff',
          fontSize: '16px',
          fontWeight: 'bold'
        },
        verticalAlign: 'bottom'
      },
      tooltip: {
        pointFormat: '{series.name}: <b>{point.percentage:.1f}%</b>'
      },
      accessibility: {
        point: {
          valueSuffix: '%'
        }
      },
      plotOptions: {
        pie: {
          allowPointSelect: false,
          cursor: 'pointer',
          dataLabels: {
            enabled: true,
            format: '<b>{point.name}</b>: {point.percentage:.1f} %'
          },
          // colors,
          point: {
            events: {
              click: (e) => {
                if (e.point.address) {
                  window.open(`https://etherscan.io/address/${e.point.address}`, '_blank');
                }
              }
            }
          }
        }
      },
      series: [{
        name: 'Holder Share',
        colorByPoint: true,
        data: []
      }]
    });


    const timezoneOffset = new Date().getTimezoneOffset();
    this.lineChart = Highcharts.chart('line-chart-container', {
      chart: {
        zoomType: 'x',
        backgroundColor: 'rgba(0,0,0,0)'
      },
      title: {
        text: 'Score vs Price',
        align: 'center',
        style: {
          color: '#fff',
          fontSize: '16px',
          fontWeight: 'bold'
        },
        verticalAlign: 'bottom'
      },
      credits: {
        enabled: false
      },
      subtitle: {
        text: document.ontouchstart === undefined ?
          'Click and drag in the plot area to zoom in' : 'Pinch the chart to zoom in',
        align: 'left'
      },
      xAxis: {
        type: 'datetime',
        labels: {
          style: {
            color: '#ffffff' // Set the text color to white
          }
        }
      },
      yAxis: [{ // first y-axis for series1
        title: {
          text: 'Score',
          style: {
            color: 'cold' // Set the text color to white
          }
        },
        labels: {
          style: {
            color: 'gold' // Set the text color to white
          }
        }
      }, { // second y-axis for series2
        title: {
          text: 'Price',
          style: {
            color: 'rgb(64, 175, 252)' // Set the text color to white
          }
        },
        labels: {
          style: {
            color: 'rgb(64, 175, 252)' // Set the text color to white
          }
        },
        opposite: true, // show second y-axis on right side of chart
        min: 0, // set minimum scale for second y-axis
        max: 100 // set maximum scale for second y-axis
      }],
      legend: {
        enabled: true,
        itemStyle: {
          color: 'white'
        }
      },
      plotOptions: {
        area: {
          fillColor: {
            linearGradient: {
              x1: 0,
              y1: 0,
              x2: 0,
              y2: 1
            },
            stops: [
              [0, Highcharts.getOptions().colors[0]],
              [1, Highcharts.color(Highcharts.getOptions().colors[0]).setOpacity(0).get('rgba')]
            ]
          },
          marker: {
            radius: 2
          },
          lineWidth: 3,
          states: {
            hover: {
              lineWidth: 1
            }
          },
          threshold: null
        }
      },

      series: [{
        // type: 'area',
        name: 'Score',
        data: [],
        yAxis: 0,
        color: 'gold',
        lineWidth: 3
      },
      {
        // type: 'area',
        name: 'Price',
        data: [],
        yAxis: 1,
        color: 'rgb(64, 175, 252)',
        lineWidth: 3
      }],
      time: {
        timezoneOffset: timezoneOffset
      }
    });

  }

  setPieChartData(token) {
    const totalShare = token.top100Holders.reduce((sum, holder) => {
      return sum + holder.share;
    }, 0);
    const chartData = token.top100Holders
      .map(holder => {
        return {
          name: `0x${holder.address.substring(2, 5)}..${holder.address.slice(-3)}`,
          address: holder.address,
          y: holder.share
        };
      });
    chartData.push({
      name: 'others',
      y: 100 - totalShare
    });
    this.pieChart.series[0].update({ data: chartData });
  }

  setScoreData() {
    if (!this.item) return;
    const scoreData = this.item.scoreDates.map(_ => {
      return [
        _.moment.valueOf(),
        _.score
      ];
    });

    this.lineChart.yAxis[0].update({
      min: scoreData.reduce((max, item) => {
        return Math.min(max, item[1]);
      }, 0),
      max: scoreData.reduce((max, item) => {
        return Math.max(max, item[1]);
      }, 0)
    });
    this.lineChart.series[0].update({ data: scoreData });
  }

  setPriceData(token) {
    const priceData = token.prices.map(_ => {
      return [
        moment(new Date(_.date)),
        _.value
      ]
    })
      .filter(_ => {
        return _[0].isAfter(this.item.scoreDates[0].moment) && _[1];
      })
      .map(_ => {
        return [
          _[0].valueOf(),
          _[1]
        ];
      });

    this.lineChart.yAxis[1].update({
      min: priceData.reduce((max, item) => {
        return Math.min(max, item[1]);
      }, 0),
      max: priceData.reduce((max, item) => {
        return Math.max(max, item[1]);
      }, 0)
    });
    this.lineChart.series[1].update({ data: priceData });
  }

}
