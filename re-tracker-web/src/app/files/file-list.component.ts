import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService, FileDto } from '../core/api.service';
import { CopyButtonComponent } from '../shared/copy-button.component';

@Component({
  selector: 'app-file-list',
  standalone: true,
  imports: [CommonModule, CopyButtonComponent],
  template: `
    <div class="page-header">
      <h2 class="f3">Files</h2>
      <span class="Counter ml-auto">{{ files.length }}</span>
    </div>

    <div class="Box">
      <table class="gh-table" style="width:100%">
        <thead>
          <tr>
            <th>Path</th>
            <th>Language</th>
            <th style="text-align:right">Methods</th>
            <th style="text-align:right">Done</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          @for (f of files; track f.id) {
            <tr>
              <td class="text-mono f6 truncate" style="max-width:420px">{{ f.relativePath }}</td>
              <td class="text-muted f6">{{ f.languageName }}</td>
              <td class="text-muted f6" style="text-align:right">{{ f.methodCount }}</td>
              <td style="text-align:right">
                <span [style.color]="f.doneCount === f.methodCount && f.methodCount > 0 ? 'var(--color-success-fg)' : 'var(--color-fg-muted)'"
                      class="f6">{{ f.doneCount }}</span>
              </td>
              <td style="width:32px"><app-copy-button [text]="f.relativePath" /></td>
            </tr>
          } @empty {
            <tr><td colspan="5" class="text-muted" style="text-align:center;padding:24px">No files indexed. Run a scan first.</td></tr>
          }
        </tbody>
      </table>
    </div>
  `
})
export class FileListComponent implements OnInit {
  private api = inject(ApiService);
  files: FileDto[] = [];

  ngOnInit() { this.api.getFiles().subscribe(f => this.files = f); }
}
