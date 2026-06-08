import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface SummaryDto {
  totalMethods: number; totalFiles: number; totalClasses: number; totalMilestones: number;
  byStatus: Record<string, number>;
  overallProgress: number;
  milestoneProgress: { id: number; name: string; total: number; done: number; progress: number }[];
}

export interface MethodSummaryDto {
  id: number; currentName: string; originalName: string; returnType: string;
  status: string; startLine: number; filePath: string;
}

export interface MethodDetailDto extends MethodSummaryDto {
  statusComment: string | null; startColumn: number; endLine: number; endColumn: number;
  parameters: MethodParameterDto[];
  callers: MethodSummaryDto[]; callees: MethodSummaryDto[];
  renameHistory: RenameHistoryDto[];
  portedName: string | null; portedPath: string | null;
}

export interface MethodParameterDto {
  id: number; currentName: string; originalName: string;
  type: string; ordinal: number; startLine: number; startColumn: number;
}

export interface UpdateStatusRequest { status: string; comment?: string; }

export interface MilestoneDto {
  id: number; name: string; description: string | null; projectId: number;
  parentId: number | null; sortOrder: number;
  totalMethods: number; doneMethods: number; progress: number;
  byStatus?: Record<string, number>;
}

export interface CallTreeNodeDto {
  id: number; currentName: string; status: string;
  filePath: string; startLine: number; cyclic: boolean;
  children: CallTreeNodeDto[];
}

export interface MilestoneTreeDto extends MilestoneDto {
  children: MilestoneTreeDto[];
}

export interface FileDto {
  id: number; relativePath: string; projectId: number;
  languageName: string; methodCount: number; doneCount: number;
}

export interface GraphDto {
  nodes: GraphNodeDto[];
  edges: GraphEdgeDto[];
}
export interface GraphNodeDto { id: number; name: string; status: string; }
export interface GraphEdgeDto { source: number; target: number; }

export interface SearchResultDto { items: SearchResultItem[]; total: number; }
export interface SearchResultItem {
  type: string; id: number; name: string; filePath: string;
  status: string | null; startLine: number | null;
}

export interface PagedResult<T> { items: T[]; totalCount: number; page: number; pageSize: number; }

export interface RenameHistoryDto {
  id: number; entityType: string; oldName: string; newName: string;
  oldFilePath: string | null; newFilePath: string | null;
  timestamp: string; comment: string | null;
}

@Injectable({ providedIn: 'root' })
export class ApiService {
  private http = inject(HttpClient);
  private base = '/api';

  getSummary(): Observable<SummaryDto> {
    return this.http.get<SummaryDto>(`${this.base}/summary`);
  }

  getMethods(params: Record<string, any> = {}): Observable<PagedResult<MethodSummaryDto>> {
    let hp = new HttpParams();
    for (const [k, v] of Object.entries(params)) if (v !== null && v !== undefined) hp = hp.set(k, String(v));
    return this.http.get<PagedResult<MethodSummaryDto>>(`${this.base}/methods`, { params: hp });
  }

  getMethod(id: number): Observable<MethodDetailDto> {
    return this.http.get<MethodDetailDto>(`${this.base}/methods/${id}`);
  }

  updateStatus(id: number, req: UpdateStatusRequest): Observable<void> {
    return this.http.put<void>(`${this.base}/methods/${id}/status`, req);
  }

  setPort(id: number, req: { portedName?: string | null; portedPath?: string | null }): Observable<MethodSummaryDto> {
    return this.http.put<MethodSummaryDto>(`${this.base}/methods/${id}/port`, req);
  }

  getMilestones(): Observable<MilestoneDto[]> {
    return this.http.get<MilestoneDto[]>(`${this.base}/milestones`);
  }

  getMilestoneTree(): Observable<MilestoneTreeDto[]> {
    return this.http.get<MilestoneTreeDto[]>(`${this.base}/milestones/tree`);
  }

  getMilestone(id: number): Observable<MilestoneDto> {
    return this.http.get<MilestoneDto>(`${this.base}/milestones/${id}`);
  }

  getMilestoneNext(id: number): Observable<MethodSummaryDto> {
    return this.http.get<MethodSummaryDto>(`${this.base}/milestones/${id}/next`);
  }

  getMilestoneMethods(id: number, pageSize = 1000): Observable<PagedResult<MethodSummaryDto>> {
    const hp = new HttpParams().set('pageSize', String(pageSize));
    return this.http.get<PagedResult<MethodSummaryDto>>(`${this.base}/milestones/${id}/methods`, { params: hp });
  }

  getMilestoneGraph(id: number): Observable<GraphDto> {
    return this.http.get<GraphDto>(`${this.base}/milestones/${id}/graph`);
  }

  getMilestoneCallTree(id: number): Observable<CallTreeNodeDto[]> {
    return this.http.get<CallTreeNodeDto[]>(`${this.base}/milestones/${id}/calltree`);
  }

  getFiles(projectId?: number): Observable<FileDto[]> {
    const params: Record<string, string> = {};
    if (projectId) params['projectId'] = String(projectId);
    return this.http.get<FileDto[]>(`${this.base}/files`, { params });
  }

  search(q: string, projectId?: number, limit = 20): Observable<SearchResultDto> {
    let hp = new HttpParams().set('q', q).set('limit', String(limit));
    if (projectId) hp = hp.set('projectId', String(projectId));
    return this.http.get<SearchResultDto>(`${this.base}/search`, { params: hp });
  }
}
