export interface Manufacturer {
  id: string;
  name: string;
  slug: string;
  description?: string | null;
  website?: string | null;
  logoUrl?: string | null;
  coverImageUrl?: string | null;
  seoTitle?: string | null;
  seoDescription?: string | null;
  seoKeywords?: string | null;
  displayOrder: number;
  isActive: boolean;
  isPublished: boolean;
  isFeatured: boolean;
  productCount: number;
}

export interface CreateManufacturerDto {
  name: string;
  description?: string | null;
  website?: string | null;
  logoUrl?: string | null;
  coverImageUrl?: string | null;
  seoTitle?: string | null;
  seoDescription?: string | null;
  seoKeywords?: string | null;
}

export interface UpdateManufacturerDto extends CreateManufacturerDto {
  slug?: string | null;
  isActive: boolean;
  isPublished: boolean;
  isFeatured: boolean;
  displayOrder: number;
}
