export interface User {
  id: string;
  name: string;
  email: string;
  role: 'Admin' | 'ProjectManager';
  createdAt: string;
}

export interface Developer {
  id?: string;
  name: string;
  email: string;
  skills: string[];
  experienceYears: number;
  availability: 'Available' | 'Busy' | 'OnLeave';
  currentWorkload: number;
  bio: string;
  avatarUrl?: string;
  createdAt?: string;
  isActive?: boolean;
  assignedProjects?: string[];
}

export interface Project {
  id?: string;
  name: string;
  description: string;
  status: 'Active' | 'OnHold' | 'Completed' | 'Cancelled';
  createdBy?: string;
  createdAt?: string;
  updatedAt?: string;
  dueDate?: string;
  requirementsText?: string;
  requirementsFileName?: string;
  aiAnalyzed?: boolean;
  projectSummary?: string;
  techStack?: string[];
  totalEstimatedHours?: number;
  assignedDeveloperId?: string;
  assignedDeveloperName?: string;
}

export interface TaskItem {
  id?: string;
  projectId: string;
  title: string;
  description: string;
  category: 'Frontend' | 'Backend' | 'Full Stack' | 'Testing' | 'DevOps' | 'Design';
  skillsRequired: string[];
  estimatedHours: number;
  priority: 'Low' | 'Medium' | 'High';
  complexity: 'Low' | 'Medium' | 'High';
  status: 'Pending' | 'Assigned' | 'InProgress' | 'Completed' | 'Cancelled';
  assignedDeveloperId?: string;
  assignedDeveloperName?: string;
  aiRecommendations?: AiRecommendation[];
  createdAt?: string;
  updatedAt?: string;
  completedAt?: string;
}

export interface AiRecommendation {
  developerId: string;
  developerName: string;
  score: number;
  matchedSkills: string[];
  missingSkills: string[];
  reason: string;
}

export interface Assignment {
  id?: string;
  taskId: string;
  taskTitle: string;
  projectId: string;
  projectName: string;
  developerId: string;
  developerName: string;
  assignedBy: string;
  assignedByName: string;
  assignedAt: string;
  status: 'Assigned' | 'InProgress' | 'Completed' | 'Cancelled' | 'Reassigned';
  notes: string;
  aiAssisted: boolean;
  aiScore?: number;
  history: AssignmentHistoryEntry[];
}

export interface AssignmentHistoryEntry {
  action: string;
  fromDeveloperId?: string;
  fromDeveloperName?: string;
  toDeveloperId?: string;
  toDeveloperName?: string;
  performedBy: string;
  performedAt: string;
  reason: string;
}

export interface DeveloperScore {
  developerId: string;
  developerName: string;
  score: number;
  skillMatchScore: number;
  experienceScore: number;
  availabilityScore: number;
  workloadScore: number;
  matchedSkills: string[];
  missingSkills: string[];
  recommendationReason: string;
}

export interface MatchResult {
  taskTitle: string;
  recommendations: DeveloperScore[];
  bestMatch?: DeveloperScore;
}

export interface DashboardStats {
  totalProjects: number;
  activeProjects: number;
  totalDevelopers: number;
  availableDevelopers: number;
  recentProjects: RecentProject[];
}

export interface RecentProject {
  projectId: string;
  projectName: string;
  status: string;
  priority: string;
  clientName: string;
  assignedDeveloperName?: string;
  createdAt: string;
}

export interface CategoryBreakdown {
  category: string;
  count: number;
}

export interface AuthResponse {
  token: string;
  userId: string;
  name: string;
  email: string;
  role: string;
  expiresAt: string;
}

export interface PagedResult<T> {
  data: T[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
