# Voluntro

Voluntro helps churches, houses of worship and other volunteer-based organizations keep track of their people, groups, events and volunteers.

## Installation & modules

**Organization**:
The church or volunteer-based organization that an Installation serves.
_Avoid_: Tenant, Church (as a general term), Customer

**Installation**:
One running deployment of Voluntro, serving exactly one Organization.
_Avoid_: Instance, Tenant, Site

**Module**:
A self-contained feature area of Voluntro, such as Check-in or Small groups. Each one is either a **Core module**, which is always on, or an **Optional module**, which the Organization can enable or disable.
_Avoid_: Feature, Plugin, App

**Setup**:
The one-time process on a new Installation that records the Organization's profile and creates its first Administrator.
_Avoid_: Onboarding, Install wizard

## People & access

**User**:
An account that can sign in to Voluntro.
_Avoid_: Account, Login, Profile

**Person**:
Someone the Organization keeps a record of, such as a member, visitor, child or guardian, whether or not they can sign in. A Person may be linked to at most one User.
_Avoid_: Member (for the record itself), Contact

**Display name**:
The name a User chooses to be shown by in Voluntro. It is not the Person's legal name.
_Avoid_: Username, Nickname

**Role**:
A named set of Permissions that can be given to Users.
_Avoid_: Group, Profile

**Permission**:
The right to perform one kind of action in one Module, anywhere in the Organization. Whether someone may act on a *specific* thing, such as a particular group or child, is decided by that thing's Module, not by a Permission.
_Avoid_: Claim, Right, Scope, Access level

**Administrator**:
The built-in Role that holds every Permission. An Organization always has at least one active Administrator.
_Avoid_: Admin user, Superuser, Owner

**Basic**:
The built-in Role every new User receives, holding only self-service Permissions such as managing their own profile and Sessions. Having it says nothing about church membership.
_Avoid_: Member (as a Role name), Participant, Default role, Guest

**Invitation**:
An offer, sent by email by someone in the Organization, to create a User for that email address.
_Avoid_: Invite code, Signup link

**Self-registration**:
Creating a User without an Invitation. It is only possible when the Organization allows it.
_Avoid_: Sign-up (as a noun), Open registration

## Signing in

**Sign-in method**:
A way for a User to prove who they are: a password or a Passkey. A User always keeps at least one.
_Avoid_: Credential, Login method

**Passkey**:
A Sign-in method stored on the User's device or password manager. It replaces a password, and needs no second factor.
_Avoid_: WebAuthn key, Security key

**Two-factor authentication (2FA)**:
A code from an authenticator app, required after the password for Users who have turned it on.
_Avoid_: MFA (in UI text), OTP, 2-step

**Recovery code**:
A single-use code that replaces a 2FA code when the authenticator app is lost.
_Avoid_: Backup code

**Session**:
One device or browser where a User is signed in. The User can see their Sessions and end any of them.
_Avoid_: Login, Token, Device

## Flagged ambiguities

- **"Member"** must never name a Role. It is reserved for a Person who formally belongs to the church (membership status, Membership module). The default Role is **Basic**.
