import { customElement, inject } from "aurelia-framework";

@inject(Element)
@customElement("scrolling-marquee")
export class ScrollingMarquee {
  constructor(element) {
    this.element = element;
  }

  attached() {
    this.marquee = document.getElementById('marquee');
    this.speed = 3;
    this.direction = 1;
    this.waitTime = 1998;

    this.setTimeout();
  }

  detached() {
    clearInterval(this.interval);
    clearTimeout(this.timeout);
  }

  start() {
    this.interval = setInterval(() => {
      this.move();
    }, 20);
  }

  stop() {
    clearInterval(this.interval);
  }

  setTimeout() {
    this.timeout = setTimeout(() => {
      this.start();
    }, this.waitTime);
  }

  resetTimeout() {
    this.stop();
    clearTimeout(this.timeout);
    this.setTimeout();
  }

  move() {

    marquee.scrollLeft += this.speed * this.direction;

    if (marquee.scrollLeft >= (marquee.scrollWidth - marquee.clientWidth)) {
      this.direction = -1;

      this.resetTimeout();
    }
    else if (marquee.scrollLeft <= 0) {
      this.direction = 1;

      this.resetTimeout();
    }
  }
}
