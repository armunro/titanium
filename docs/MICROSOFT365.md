# Microsoft 365 Outlook Integration

## Overview

Titanium integrates with Microsoft 365 Outlook using Microsoft Graph SDK and MSAL (Microsoft Authentication Library).

## Scopes & Permissions

- `User.Read`: Retrieve profile details and primary mailbox email.
- `Mail.Read`: Read emails and metadata without write access.
- `offline_access`: Enable refresh token caching.

## Token Caching

Tokens are cached using `MsalCacheHelper` under `%LOCALAPPDATA%\Titanium\MsalCache` with protected local storage.

## Synchronization Pipeline

1. **Authentication**: Interactive MSAL sign-in / silent refresh.
2. **Fetch**: Query Microsoft Graph mail folders / delta messages.
3. **Normalization**: HTML stripping, thread pruning, signature stripping, content SHA-256 calculation.
4. **Duplicate Detection**: Deduplication by Graph Message ID and SHA-256 hash.
5. **Thread Resolution**: Automatic grouping into logical `Conversation` entities by normalized subject.
