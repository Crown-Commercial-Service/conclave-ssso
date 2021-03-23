import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';

@Component({
  selector: 'app-govuk-table',
  templateUrl: './govuk-table.component.html',
  styleUrls: ['./govuk-table.component.scss']
})
export class GovUKTableComponent {

  @Input() headerTextKeys?: string[];
  @Input() data?: any[];
  @Input() dataKeys?: string[];
  @Input() isEditVisible?: boolean;

  @Output() editRowEvent = new EventEmitter<any>();

  constructor() {
  }

  ngOnInit() {
  }

  onEditClick(dataRow: any) {
    this.editRowEvent.emit(dataRow);
  }
}
