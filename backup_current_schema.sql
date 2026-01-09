ALTER DATABASE CHARACTER SET utf8mb4;


CREATE TABLE `AspNetRoles` (
    `Id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Name` varchar(256) CHARACTER SET utf8mb4 NULL,
    `NormalizedName` varchar(256) CHARACTER SET utf8mb4 NULL,
    `ConcurrencyStamp` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_AspNetRoles` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `AspNetUsers` (
    `Id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `UserName` varchar(256) CHARACTER SET utf8mb4 NULL,
    `NormalizedUserName` varchar(256) CHARACTER SET utf8mb4 NULL,
    `Email` varchar(256) CHARACTER SET utf8mb4 NULL,
    `NormalizedEmail` varchar(256) CHARACTER SET utf8mb4 NULL,
    `EmailConfirmed` tinyint(1) NOT NULL,
    `PasswordHash` longtext CHARACTER SET utf8mb4 NULL,
    `SecurityStamp` longtext CHARACTER SET utf8mb4 NULL,
    `ConcurrencyStamp` longtext CHARACTER SET utf8mb4 NULL,
    `PhoneNumber` longtext CHARACTER SET utf8mb4 NULL,
    `PhoneNumberConfirmed` tinyint(1) NOT NULL,
    `TwoFactorEnabled` tinyint(1) NOT NULL,
    `LockoutEnd` datetime(6) NULL,
    `LockoutEnabled` tinyint(1) NOT NULL,
    `AccessFailedCount` int NOT NULL,
    CONSTRAINT `PK_AspNetUsers` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `IdempotencyKeys` (
    `Key` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `ResponseBody` longtext CHARACTER SET utf8mb4 NOT NULL,
    `StatusCode` int NOT NULL,
    `ContentType` longtext CHARACTER SET utf8mb4 NOT NULL,
    `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT `PK_IdempotencyKeys` PRIMARY KEY (`Key`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `Tours` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `Title` longtext CHARACTER SET utf8mb4 NOT NULL,
    `StartAt` datetime(6) NOT NULL,
    `StartLocationText` longtext CHARACTER SET utf8mb4 NOT NULL,
    `EndLocationText` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Status` int NOT NULL,
    `Notes` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_Tours` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `AspNetRoleClaims` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `RoleId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `ClaimType` longtext CHARACTER SET utf8mb4 NULL,
    `ClaimValue` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_AspNetRoleClaims` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_AspNetRoleClaims_AspNetRoles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `AspNetRoles` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;


CREATE TABLE `AppUserRoles` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `UserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Role` int NOT NULL,
    CONSTRAINT `PK_AppUserRoles` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_AppUserRoles_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;


CREATE TABLE `AspNetUserClaims` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `UserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `ClaimType` longtext CHARACTER SET utf8mb4 NULL,
    `ClaimValue` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_AspNetUserClaims` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_AspNetUserClaims_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;


CREATE TABLE `AspNetUserLogins` (
    `LoginProvider` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `ProviderKey` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `ProviderDisplayName` longtext CHARACTER SET utf8mb4 NULL,
    `UserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_AspNetUserLogins` PRIMARY KEY (`LoginProvider`, `ProviderKey`),
    CONSTRAINT `FK_AspNetUserLogins_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;


CREATE TABLE `AspNetUserRoles` (
    `UserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `RoleId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_AspNetUserRoles` PRIMARY KEY (`UserId`, `RoleId`),
    CONSTRAINT `FK_AspNetUserRoles_AspNetRoles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `AspNetRoles` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_AspNetUserRoles_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;


CREATE TABLE `AspNetUserTokens` (
    `UserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `LoginProvider` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Name` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Value` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_AspNetUserTokens` PRIMARY KEY (`UserId`, `LoginProvider`, `Name`),
    CONSTRAINT `FK_AspNetUserTokens_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;


CREATE TABLE `MotardProfiles` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `UserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Bio` longtext CHARACTER SET utf8mb4 NULL,
    `BikeModel` longtext CHARACTER SET utf8mb4 NULL,
    `LicenseNo` longtext CHARACTER SET utf8mb4 NOT NULL,
    `AvgRating` decimal(65,30) NULL,
    `CompletedTours` int NOT NULL,
    CONSTRAINT `PK_MotardProfiles` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_MotardProfiles_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;


CREATE TABLE `RateLimitCounters` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `ParticipantUserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Date` date NOT NULL,
    `NewConversationsCount` int NOT NULL,
    `ParticipantId` varchar(255) CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_RateLimitCounters` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_RateLimitCounters_AspNetUsers_ParticipantId` FOREIGN KEY (`ParticipantId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;


CREATE TABLE `PreBookingConversations` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `TourId` char(36) COLLATE ascii_general_ci NOT NULL,
    `MotardUserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `ParticipantUserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_PreBookingConversations` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_PreBookingConversations_AspNetUsers_MotardUserId` FOREIGN KEY (`MotardUserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_PreBookingConversations_AspNetUsers_ParticipantUserId` FOREIGN KEY (`ParticipantUserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_PreBookingConversations_Tours_TourId` FOREIGN KEY (`TourId`) REFERENCES `Tours` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;


CREATE TABLE `TourMotards` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `TourId` char(36) COLLATE ascii_general_ci NOT NULL,
    `MotardUserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `CapacitySeats` int NOT NULL,
    `PricePerSeat` decimal(10,2) NOT NULL,
    `SeatsBooked` int NOT NULL,
    `RowVersion` datetime(6) NOT NULL,
    `MotardProfileId` char(36) COLLATE ascii_general_ci NULL,
    CONSTRAINT `PK_TourMotards` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_TourMotards_AspNetUsers_MotardUserId` FOREIGN KEY (`MotardUserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_TourMotards_MotardProfiles_MotardProfileId` FOREIGN KEY (`MotardProfileId`) REFERENCES `MotardProfiles` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_TourMotards_Tours_TourId` FOREIGN KEY (`TourId`) REFERENCES `Tours` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;


CREATE TABLE `Bookings` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `TourId` char(36) COLLATE ascii_general_ci NOT NULL,
    `TourMotardId` char(36) COLLATE ascii_general_ci NOT NULL,
    `ParticipantUserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Status` int NOT NULL,
    `CancelDecision` int NOT NULL,
    `CostSharingOptIn` tinyint(1) NOT NULL,
    `EmergencyContact` longtext CHARACTER SET utf8mb4 NULL,
    `MessageToMotard` longtext CHARACTER SET utf8mb4 NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `ApprovedAt` datetime(6) NULL,
    `ExpiresAt` datetime(6) NULL,
    `PaidAt` datetime(6) NULL,
    `CancelledAt` datetime(6) NULL,
    `RefundedAt` datetime(6) NULL,
    `DeclinedAt` datetime(6) NULL,
    `DeclineReason` longtext CHARACTER SET utf8mb4 NULL,
    `RowVersion` datetime(6) NOT NULL,
    CONSTRAINT `PK_Bookings` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Bookings_AspNetUsers_ParticipantUserId` FOREIGN KEY (`ParticipantUserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Bookings_TourMotards_TourMotardId` FOREIGN KEY (`TourMotardId`) REFERENCES `TourMotards` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Bookings_Tours_TourId` FOREIGN KEY (`TourId`) REFERENCES `Tours` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;


CREATE TABLE `BookingConversations` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `BookingId` char(36) COLLATE ascii_general_ci NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_BookingConversations` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_BookingConversations_Bookings_BookingId` FOREIGN KEY (`BookingId`) REFERENCES `Bookings` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;


CREATE TABLE `Payments` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `BookingId` char(36) COLLATE ascii_general_ci NOT NULL,
    `Amount` decimal(10,2) NOT NULL,
    `Status` int NOT NULL,
    `ProviderRef` longtext CHARACTER SET utf8mb4 NULL,
    `PaidAt` datetime(6) NULL,
    `CreatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_Payments` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Payments_Bookings_BookingId` FOREIGN KEY (`BookingId`) REFERENCES `Bookings` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;


CREATE TABLE `Payouts` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `BookingId` char(36) COLLATE ascii_general_ci NOT NULL,
    `PlatformShare` decimal(10,2) NOT NULL,
    `MotardShare` decimal(10,2) NOT NULL,
    `Status` int NOT NULL,
    `SettledAt` datetime(6) NULL,
    `CreatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_Payouts` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Payouts_Bookings_BookingId` FOREIGN KEY (`BookingId`) REFERENCES `Bookings` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;


CREATE TABLE `Reviews` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `BookingId` char(36) COLLATE ascii_general_ci NOT NULL,
    `ReviewerUserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `TargetType` int NOT NULL,
    `TargetId` varchar(450) CHARACTER SET utf8mb4 NOT NULL,
    `Rating` int NOT NULL,
    `Comment` longtext CHARACTER SET utf8mb4 NULL,
    `CreatedAtUtc` datetime(6) NOT NULL,
    `BookingId1` char(36) COLLATE ascii_general_ci NULL,
    CONSTRAINT `PK_Reviews` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Reviews_AspNetUsers_ReviewerUserId` FOREIGN KEY (`ReviewerUserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Reviews_Bookings_BookingId` FOREIGN KEY (`BookingId`) REFERENCES `Bookings` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Reviews_Bookings_BookingId1` FOREIGN KEY (`BookingId1`) REFERENCES `Bookings` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;


CREATE TABLE `Messages` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `PreBookingConversationId` char(36) COLLATE ascii_general_ci NULL,
    `BookingConversationId` char(36) COLLATE ascii_general_ci NULL,
    `SenderUserId` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Content` longtext CHARACTER SET utf8mb4 NOT NULL,
    `SentAt` datetime(6) NOT NULL,
    `SenderId` varchar(255) CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_Messages` PRIMARY KEY (`Id`),
    CONSTRAINT `CK_Message_XOR` CHECK (((PreBookingConversationId IS NOT NULL AND BookingConversationId IS NULL) OR (PreBookingConversationId IS NULL AND BookingConversationId IS NOT NULL))),
    CONSTRAINT `FK_Messages_AspNetUsers_SenderId` FOREIGN KEY (`SenderId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Messages_BookingConversations_BookingConversationId` FOREIGN KEY (`BookingConversationId`) REFERENCES `BookingConversations` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Messages_PreBookingConversations_PreBookingConversationId` FOREIGN KEY (`PreBookingConversationId`) REFERENCES `PreBookingConversations` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;


CREATE TABLE `PaymentKey` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `BookingId` char(36) COLLATE ascii_general_ci NOT NULL,
    `IdempotencyKey` longtext CHARACTER SET utf8mb4 NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `PaymentId` char(36) COLLATE ascii_general_ci NULL,
    CONSTRAINT `PK_PaymentKey` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_PaymentKey_Bookings_BookingId` FOREIGN KEY (`BookingId`) REFERENCES `Bookings` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_PaymentKey_Payments_PaymentId` FOREIGN KEY (`PaymentId`) REFERENCES `Payments` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;


CREATE TABLE `ReviewReplies` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `ReviewId` char(36) COLLATE ascii_general_ci NOT NULL,
    `AuthorUserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Message` varchar(1000) CHARACTER SET utf8mb4 NOT NULL,
    `CreatedAtUtc` datetime(6) NOT NULL,
    CONSTRAINT `PK_ReviewReplies` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ReviewReplies_AspNetUsers_AuthorUserId` FOREIGN KEY (`AuthorUserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_ReviewReplies_Reviews_ReviewId` FOREIGN KEY (`ReviewId`) REFERENCES `Reviews` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;


CREATE INDEX `IX_AppUserRoles_UserId` ON `AppUserRoles` (`UserId`);


CREATE INDEX `IX_AspNetRoleClaims_RoleId` ON `AspNetRoleClaims` (`RoleId`);


CREATE UNIQUE INDEX `RoleNameIndex` ON `AspNetRoles` (`NormalizedName`);


CREATE INDEX `IX_AspNetUserClaims_UserId` ON `AspNetUserClaims` (`UserId`);


CREATE INDEX `IX_AspNetUserLogins_UserId` ON `AspNetUserLogins` (`UserId`);


CREATE INDEX `IX_AspNetUserRoles_RoleId` ON `AspNetUserRoles` (`RoleId`);


CREATE INDEX `EmailIndex` ON `AspNetUsers` (`NormalizedEmail`);


CREATE UNIQUE INDEX `UserNameIndex` ON `AspNetUsers` (`NormalizedUserName`);


CREATE UNIQUE INDEX `IX_BookingConversations_BookingId` ON `BookingConversations` (`BookingId`);


CREATE INDEX `IX_Bookings_ParticipantUserId` ON `Bookings` (`ParticipantUserId`);


CREATE INDEX `IX_Bookings_TourId` ON `Bookings` (`TourId`);


CREATE INDEX `IX_Bookings_TourMotardId` ON `Bookings` (`TourMotardId`);


CREATE UNIQUE INDEX `IX_IdempotencyKeys_Key` ON `IdempotencyKeys` (`Key`);


CREATE INDEX `IX_Messages_BookingConversationId` ON `Messages` (`BookingConversationId`);


CREATE INDEX `IX_Messages_PreBookingConversationId` ON `Messages` (`PreBookingConversationId`);


CREATE INDEX `IX_Messages_SenderId` ON `Messages` (`SenderId`);


CREATE UNIQUE INDEX `IX_MotardProfiles_UserId` ON `MotardProfiles` (`UserId`);


CREATE INDEX `IX_PaymentKey_BookingId` ON `PaymentKey` (`BookingId`);


CREATE INDEX `IX_PaymentKey_PaymentId` ON `PaymentKey` (`PaymentId`);


CREATE UNIQUE INDEX `IX_Payments_BookingId` ON `Payments` (`BookingId`);


CREATE UNIQUE INDEX `IX_Payouts_BookingId` ON `Payouts` (`BookingId`);


CREATE INDEX `IX_PreBookingConversations_MotardUserId` ON `PreBookingConversations` (`MotardUserId`);


CREATE INDEX `IX_PreBookingConversations_ParticipantUserId` ON `PreBookingConversations` (`ParticipantUserId`);


CREATE UNIQUE INDEX `IX_PreBookingConversations_TourId_MotardUserId_ParticipantUserId` ON `PreBookingConversations` (`TourId`, `MotardUserId`, `ParticipantUserId`);


CREATE INDEX `IX_RateLimitCounters_ParticipantId` ON `RateLimitCounters` (`ParticipantId`);


CREATE UNIQUE INDEX `IX_RateLimitCounters_ParticipantUserId_Date` ON `RateLimitCounters` (`ParticipantUserId`, `Date`);


CREATE INDEX `IX_ReviewReplies_AuthorUserId` ON `ReviewReplies` (`AuthorUserId`);


CREATE INDEX `IX_ReviewReplies_ReviewId` ON `ReviewReplies` (`ReviewId`);


CREATE UNIQUE INDEX `IX_Reviews_BookingId_ReviewerUserId_TargetType_TargetId` ON `Reviews` (`BookingId`, `ReviewerUserId`, `TargetType`, `TargetId`);


CREATE INDEX `IX_Reviews_BookingId1` ON `Reviews` (`BookingId1`);


CREATE INDEX `IX_Reviews_ReviewerUserId` ON `Reviews` (`ReviewerUserId`);


CREATE INDEX `IX_TourMotards_MotardProfileId` ON `TourMotards` (`MotardProfileId`);


CREATE INDEX `IX_TourMotards_MotardUserId` ON `TourMotards` (`MotardUserId`);


CREATE UNIQUE INDEX `IX_TourMotards_TourId_MotardUserId` ON `TourMotards` (`TourId`, `MotardUserId`);


