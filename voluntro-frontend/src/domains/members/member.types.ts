export const LegalGender = {
  unknown: "unknown",
  male: "male",
  female: "female",
} as const;

export type LegalGenderEnum = (typeof LegalGender)[keyof typeof LegalGender];

export type MemberBrief = {
  id: string;
  firstName: string;
  middleNames: string | null;
  lastName: string;
  email: string | null;
  dateOfBirth: string;
  legalGender: LegalGenderEnum;
  tags: Tag[];
  memberTypeId: string;
  createdAt: string;
  updatedAt: string;
  isDeleted: boolean;
};

export type Member = {
  age: number;
} & MemberBrief;

export type Tag = {
  id: string;
  name: string;
  color: string;
};
