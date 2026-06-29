import { inject } from 'aurelia-framework';
import { DialogController } from 'aurelia-dialog';
import { EventAggregator } from 'aurelia-event-aggregator';
import Highcharts from 'highcharts';

@inject(DialogController, EventAggregator)
export class Notifications {

  constructor(controller, eventAggregator) {
    this.controller = controller;
    this._eventAggregator = eventAggregator;
  }

  activate(txnNotifications) {
    this.txnNotifications = txnNotifications;
  }

  attached() {
    this.txnNotificationsChanged = this._eventAggregator.subscribe('txn-notifications-changed', txnNotifications => {
      this.txnNotifications = txnNotifications;

      const tokenCreationCount = this.txnNotifications.filter(_ => _.type == 'TokenCreation').length;
      const removedLiquidityCount = this.txnNotifications.filter(_ => _.type == 'RemovedLiquidity').length;
      const renouncedOwnershipCount = this.txnNotifications.filter(_ => _.type == 'RenouncedOwnership').length;

      this.chart.series[0].update({ data: [tokenCreationCount] });
      this.chart.series[1].update({ data: [removedLiquidityCount] });
      this.chart.series[2].update({ data: [renouncedOwnershipCount] });
      
    });

    this.setChart();
  }

  detached() {
    this.txnNotificationsChanged.dispose();
  }

  ok() {
    this.controller.ok({});
  }

  typeToClass(type) {
    if (type == 'TokenCreation') return 'token-creation';
    if (type == 'RenouncedOwnership') return 'renounced-ownership';
    if (type == 'RemovedLiquidity') return 'removed-liquidity';
    return 'set-fees';
  }

  setChart() {

    const tokenCreationCount = this.txnNotifications.filter(_ => _.type == 'TokenCreation').length;
    const removedLiquidityCount = this.txnNotifications.filter(_ => _.type == 'RemovedLiquidity').length;
    const renouncedOwnershipCount = this.txnNotifications.filter(_ => _.type == 'RenouncedOwnership').length;

    this.chart = Highcharts.chart('chart-container', {
      chart: {
        type: 'column',
        backgroundColor: 'rgb(13, 12, 34)',
        style: {
          color: '#FFFFFF'
        }
      },
      credits: {
        enabled: false
      },
      title: {
        text: 'txns types in the last 30 min',
        style: {
          color: '#FFFFFF'
        }
      },
      xAxis: {
        categories: ['', '', '']
      },
      yAxis: {
        title: {
          text: '',
          style: {
            color: '#FFFFFF'
          }
        },
        labels: {
          style: {
            color: '#FFFFFF'
          }
        }
      },
      legend: {
        itemStyle: {
          color: '#FFFFFF'
        }
      },
      plotOptions: {
        column: {
          dataLabels: {
            enabled: true,
            format: '{y}', 
            style: {
              fontWeight: 'bold' ,
              fontSize: '18px'
            },
            color: 'yellow'
          }
        }
      },
      series: [
        {
          name: 'TokenCreation',
          data: [tokenCreationCount],
          color: 'rgb(5, 158, 253)'
        },
        {
          name: 'RemovedLiquidity',
          data: [removedLiquidityCount],
          color: '#FDAD33'
        },
        {
          name: 'RenouncedOwnership',
          data: [renouncedOwnershipCount],
          color: '#9CCC3D'
        }
      ]
    });
  }

}
