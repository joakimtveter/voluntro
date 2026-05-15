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
  dateOfBirth: string;
  legalGender: LegalGenderEnum;
  createdAt: string;
  updatedAt: string;
};

export type Member = {
  age: number;
} & MemberBrief;
