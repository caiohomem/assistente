export interface Address {
  street?: string | null;
  city?: string | null;
  state?: string | null;
  zipCode?: string | null;
  country?: string | null;
}

export interface Relationship {
  relationshipId: string;
  sourceContactId: string;
  targetContactId: string;
  type: string;
  relationshipTypeId?: string | null;
  description?: string | null;
  strength: number;
  isConfirmed: boolean;
}

export interface Contact {
  contactId: string;
  ownerUserId: string;
  firstName: string;
  lastName?: string | null;
  fullName: string;
  jobTitle?: string | null;
  company?: string | null;
  emails: string[];
  phones: string[];
  address?: Address | null;
  tags: string[];
  relationships: Relationship[];
  createdAt: string;
  updatedAt: string;
}

export interface CreateContactRequest {
  firstName: string;
  lastName?: string | null;
  jobTitle?: string | null;
  company?: string | null;
  emails?: string[];
  phones?: string[];
  address?: Address | null;
  tags?: string[];
}

export interface UpdateContactRequest {
  firstName?: string;
  lastName?: string | null;
  jobTitle?: string | null;
  company?: string | null;
  emails?: string[];
  phones?: string[];
  address?: Address | null;
  tags?: string[];
}

export interface AddContactEmailRequest {
  email: string;
}

export interface AddContactPhoneRequest {
  phone: string;
}

export interface AddContactTagRequest {
  tag: string;
}

export interface AddContactRelationshipRequest {
  targetContactId: string;
  type?: string;
  relationshipTypeId?: string;
  description?: string | null;
  strength?: number;
  isConfirmed?: boolean;
}

export interface GraphNode {
  contactId: string;
  fullName: string;
  company?: string | null;
  jobTitle?: string | null;
  primaryEmail?: string | null;
}

export interface GraphEdge {
  relationshipId: string;
  sourceContactId: string;
  targetContactId: string;
  type: string;
  description?: string | null;
  strength: number;
  isConfirmed: boolean;
}

export interface NetworkGraph {
  nodes: GraphNode[];
  edges: GraphEdge[];
}









