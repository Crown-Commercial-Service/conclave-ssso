import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { Store } from '@ngrx/store';
import { TranslateService } from '@ngx-translate/core';
import { UIState } from 'src/app/store/ui.states';
import { environment } from 'src/environments/environment';
import { BaseComponent } from '../base/base.component';

@Component({
  selector: 'app-govuk-table',
  templateUrl: './govuk-table.component.html',
  styleUrls: ['./govuk-table.component.scss']
})
export class GovUKTableComponent extends BaseComponent implements OnInit {

  @Input() headerTextKeys!: string[];
  @Input() data!: any[];
  @Input() dataKeys!: string[];
  @Input() isHyperLinkVisible?: boolean;
  @Input() hyperLinkText?: string;
  @Input() isCheckBoxVisible?: boolean;
  @Input() useServerPagination?: boolean;
  @Input() serverPageCount?: number;
  @Input() serverPageCurrentPage?: number;
  @Input() useClientPagination?: boolean;

  @Output() hyperLinkClickEvent = new EventEmitter<any>();
  @Output() checkBoxClickEvent = new EventEmitter<any>();
  @Output() changeCurrentPageEvent = new EventEmitter<number>();

  pageCount?: number;
  currentPage: number = 1;
  totalPagesArray: number[] = [];
  pageSize: number = environment.listPageSize;
  tableVisibleData!: any[];

  constructor(
    // private translateService: TranslateService,
    protected uiStore: Store<UIState>) {
    super(uiStore);
  }

  ngOnInit() {
  }

  ngOnChanges() {
    if (this.useClientPagination) {
      this.pageCount = Math.ceil(this.data.length / this.pageSize);
      this.totalPagesArray = Array(this.pageCount).fill(0).map((x, i) => i + 1);
      this.tableVisibleData = this.data.slice(0, this.pageSize);
      this.currentPage = 1;
    }
    else {
      this.totalPagesArray = Array(this.serverPageCount).fill(0).map((x, i) => i + 1);
      this.pageCount = this.serverPageCount;
      this.tableVisibleData = this.data;
      this.currentPage = this.serverPageCurrentPage || 1;
    }
  }

  onHyperLinkClick(dataRow: any) {
    this.hyperLinkClickEvent.emit(dataRow);
  }

  onCheckChange(event: any, dataRow: any) {
    dataRow.isChecked = event.target.checked;
    this.checkBoxClickEvent.emit(dataRow);
  }

  onSetPageClick(pageNumber: number) {
    this.currentPage = pageNumber;
    if (this.useClientPagination) {
      let startIndex = this.pageSize * (this.currentPage - 1);
      let endIndex = startIndex + this.pageSize;
      this.tableVisibleData = this.data.slice(startIndex, endIndex);
    }
    else {
      this.changeCurrentPageEvent.emit(pageNumber);
    }
  }
}
