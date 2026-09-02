import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  Category,
  CreateCategoryDto,
  PagedResult,
  PaginationQuery,
  UpdateCategoryDto,
} from '../models';

@Injectable({ providedIn: 'root' })
export class CategoriesService {
  private readonly http = inject(HttpClient);

  getAll(query: PaginationQuery = { page: 1, pageSize: 50 }): Promise<PagedResult<Category>> {
    const params = { page: query.page ?? 1, pageSize: query.pageSize ?? 50 };
    return firstValueFrom(
      this.http.get<PagedResult<Category>>(`${environment.apiBaseUrl}/categories`, { params }),
    );
  }

  /** Unpaginated list for dropdowns/selects. */
  getAllUnpaginated(): Promise<Category[]> {
    return firstValueFrom(this.http.get<Category[]>(`${environment.apiBaseUrl}/categories/all`));
  }

  getById(id: string): Promise<Category> {
    return firstValueFrom(this.http.get<Category>(`${environment.apiBaseUrl}/categories/${id}`));
  }

  create(dto: CreateCategoryDto): Promise<Category> {
    return firstValueFrom(this.http.post<Category>(`${environment.apiBaseUrl}/categories`, dto));
  }

  update(id: string, dto: UpdateCategoryDto): Promise<Category> {
    return firstValueFrom(
      this.http.put<Category>(`${environment.apiBaseUrl}/categories/${id}`, dto),
    );
  }

  delete(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${environment.apiBaseUrl}/categories/${id}`));
  }
}
