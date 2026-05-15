export type GroupBrief = {
  id: string;
  name: string;
  parentGroupId: string;
  createdAt: string;
  updatedAt: string;
  isDeleted: boolean;
};

export type Group = {
  parentGroupName: string;
  description: string;
  childGroups: GroupBrief[];
  memberCount: number;
} & GroupBrief;
