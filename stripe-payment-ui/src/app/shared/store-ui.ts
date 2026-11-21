import { ChangeDetectionStrategy, Component, Directive, input, Pipe, PipeTransform } from '@angular/core';

@Directive({
  selector: 'button[appBusy]',
  host: { '[attr.aria-busy]': 'busy()', '[attr.disabled]': 'busy() || blocked() ? true : null' },
})
export class BusyButton {
  readonly busy = input(false, { alias: 'appBusy' });
  readonly blocked = input(false);
}

@Pipe({ name: 'orderReference' })
export class OrderReference implements PipeTransform {
  transform(id: string): string { return '#' + id.slice(0, 8).toUpperCase(); }
}

@Component({
  selector: 'app-loading',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<p class="loading" role="status">{{ label() }}</p>',
})
export class Loading {
  readonly label = input('Loading…');
}
