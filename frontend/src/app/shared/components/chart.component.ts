import { Component, ElementRef, Input, OnDestroy, ViewChild, afterNextRender } from '@angular/core';
import {
  BarController,
  BarElement,
  CategoryScale,
  Chart,
  ChartConfiguration,
  DoughnutController,
  ArcElement,
  Legend,
  LineController,
  LineElement,
  LinearScale,
  PointElement,
  Title,
  Tooltip,
} from 'chart.js';

// Register the Chart.js pieces used by the dashboard.
Chart.register(
  BarController,
  BarElement,
  DoughnutController,
  ArcElement,
  LineController,
  LineElement,
  CategoryScale,
  LinearScale,
  PointElement,
  Title,
  Tooltip,
  Legend,
);

/**
 * Minimal Chart.js wrapper for standalone Angular components.
 * Receives a ready ChartConfiguration via @Input and destroys the chart
 * instance on destroy (prevents memory leaks with OnPush).
 */
@Component({
  selector: 'app-chart',
  template: `<canvas #canvas></canvas>`,
  styles: [
    `
      :host {
        display: block;
        width: 100%;
        min-height: 240px;
      }
      canvas {
        max-height: 320px;
      }
    `,
  ],
})
export class ChartComponent implements OnDestroy {
  @ViewChild('canvas', { static: true }) canvas!: ElementRef<HTMLCanvasElement>;

  @Input() config: ChartConfiguration | null = null;

  private chart: Chart | undefined;

  constructor() {
    // Chart.js needs the canvas to be in the DOM; afterNextRender handles it.
    afterNextRender(() => this.render());
  }

  ngOnDestroy(): void {
    this.chart?.destroy();
  }

  private render(): void {
    this.chart?.destroy();
    if (!this.config) return;
    this.chart = new Chart(this.canvas.nativeElement, this.config);
  }
}
