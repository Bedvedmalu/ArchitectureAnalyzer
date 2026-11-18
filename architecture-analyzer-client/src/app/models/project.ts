export interface Project {
  id: number;
  projectName: string;
  uploadDate: string;
  projectLanguage: string;
}

export interface ProjectCreateRequest {
  projectName: string;
  projectUrl: string;
}

export interface ProjectUpdateRequest {
  projectName: string;
}

export interface ProjectHealth {
  projectName: string;
  healthScore: number;
  totalClassesCount: number;
  complexClassesCount: number;
}

export interface RiskyProject {
  id: number;
  projectName: string;
  riskyClassesCount: number;
}

export interface ProjectLines {
  id: number;
  projectName: string;
  avgLines: number;
  minLines: number;
  maxLines: number;
  totalLines: number;
  totalClasses: number;
}
