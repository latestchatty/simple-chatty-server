# WinChatty API

<!-- use "npx doctoc api.md" to generate this table of contents. --> 
<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->


- [Introduction](#introduction)
  - [Protocols](#protocols)
  - [Data Types](#data-types)
    - [Request and response types](#request-and-response-types)
    - [Response-only types](#response-only-types)
  - [Error Responses](#error-responses)
  - [Client Implementation Guide](#client-implementation-guide)
- [Threads](#threads)
  - [GET /v2/getChatty](#get-v2getchatty)
  - [GET /v2/getChattyRootPosts](#get-v2getchattyrootposts)
  - [GET /v2/getThread](#get-v2getthread)
  - [GET /v2/getThreadPostCount](#get-v2getthreadpostcount)
- [Posts](#posts)
  - [GET /v2/getNewestPostInfo](#get-v2getnewestpostinfo)
  - [GET /v2/getPost](#get-v2getpost)
  - [POST /v2/postComment](#post-v2postcomment)
  - [GET /v2/search](#get-v2search)
  - [POST /v2/requestReindex](#post-v2requestreindex)
  - [POST /v2/setPostCategory](#post-v2setpostcategory)
- [Events](#events)
  - [GET /v2/getNewestEventId](#get-v2getnewesteventid)
  - [GET /v2/waitForEvent](#get-v2waitforevent)
  - [GET /v2/pollForEvent](#get-v2pollforevent)
- [Users](#users)
  - [GET /v2/checkConnection](#get-v2checkconnection)
  - [POST /v2/verifyCredentials](#post-v2verifycredentials)
  - [GET /v2/getAllTenYearUsers](#get-v2getalltenyearusers)
- [Messages](#messages)
  - [POST /v2/getMessages](#post-v2getmessages)
  - [POST /v2/getMessageCount](#post-v2getmessagecount)
  - [POST /v2/sendMessage](#post-v2sendmessage)
  - [POST /v2/markMessageRead](#post-v2markmessageread)
  - [POST /v2/deleteMessage](#post-v2deletemessage)
- [Client Data](#client-data)
  - [GET /v2/clientData/getCategoryFilters](#get-v2clientdatagetcategoryfilters)
  - [POST /v2/clientData/setCategoryFilters](#post-v2clientdatasetcategoryfilters)
  - [GET /v2/clientData/getMarkedPosts](#get-v2clientdatagetmarkedposts)
  - [POST /v2/clientData/clearMarkedPosts](#post-v2clientdataclearmarkedposts)
  - [POST /v2/clientData/markPost](#post-v2clientdatamarkpost)
  - [GET /v2/clientData/getClientData](#get-v2clientdatagetclientdata)
  - [POST /v2/clientData/setClientData](#post-v2clientdatasetclientdata)
- [Legacy v1 API](#legacy-v1-api)
  - [Data Types (v1)](#data-types-v1)
  - [Error Responses (v1)](#error-responses-v1)
  - [GET /chatty/about](#get-chattyabout)
  - [GET /chatty/index.json](#get-chattyindexjson)
  - [GET /chatty/`[INT]`.`[INT]`.json](#get-chattyintintjson)
  - [GET /chatty/thread/`[INT]`.json](#get-chattythreadintjson)
  - [GET /chatty/search.json](#get-chattysearchjson)
  - [GET /chatty/stories.json](#get-chattystoriesjson)
  - [GET /chatty/stories/`[INT]`.json](#get-chattystoriesintjson)
  - [GET /chatty/stories/`[INT]`.`[INT]`.json](#get-chattystoriesintintjson)
  - [POST /chatty/messages.json](#post-chattymessagesjson)
  - [POST /chatty/messages/`[INT]`.json](#post-chattymessagesintjson)
  - [POST /chatty/messages/send/](#post-chattymessagessend)
  - [POST /chatty/post/](#post-chattypost)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

## Introduction
This documents the WebChatty API, a backend web service for a chatty-style forum.

This API implements a subset of versions 1 and 2 of the WinChatty API, allowing it to support preexisting clients of that API.

### Protocols
Client applications should be configured to use GZIP compression.  On average GZIP cuts the size of responses down by 75%.  Try calling [/v2/checkConnection](#get-v2checkconnection) to verify that you are correctly using GZIP.

The v2 API uses neither cookies nor HTTP authentication.  Usernames and passwords, when applicable, are passed via POST arguments.  It is highly recommended that HTTPS be used so that usernames and passwords are not transmitted in plain text.  You may wish to use HTTP for requests where passwords are not transmitted; in mobile clients on cellular networks, the SSL handshaking can add a significant amount of latency.

### Data Types
In order to precisely define the accepted inputs (query parameters) and the expected outputs (JSON) of the v2 API methods, the following data type shorthands are defined.  Most types appear in both query parameters and JSON responses, but a few only appear in JSON responses.

The following suffixes may appear on any of the data types below:
- The suffix + indicates a list of one or more, separated by comma.
- The suffix ? indicates that the argument may be omitted or empty.
- The suffix * is the combinaton of + and ? (i.e. a list of zero or more).
- A comma and a number indicates the maximum value for integer arguments, the maximum count for list arguments, and the maximum length for string arguments.

#### Request and response types
Type | Description
--- | ---
`[INT]` | Unsigned 31-bit decimal integer (range 0..2147483647).  No leading zeroes.
`[BIT]` | `true` or `false`
`[STR]` | String
`[DAT]` | Combined date and time, represented as a strict subset of RFC 3339, which is itself a strict subset of ISO 8601.  Dates in JSON responses will always be formatted exactly like this: `"2013-12-01T19:39:00.000Z"`.  The time is in the UTC time zone.  Make sure to convert all `[DAT]` values to the user's local time zone before displaying!
`[MOD]` | Moderation flag enum.  One of the following strings: `"ontopic"` `"nws"` `"stupid"` `"political"` `"tangent"` `"informative"`
`[MODN]` | Moderation flag enum, including "nuked".  One of the following strings: `"ontopic"` `"nws"` `"stupid"` `"political"` `"tangent"` `"informative"` `"nuked"`
`[MBX]` | Mailbox enum.  One of the following strings: `"inbox"` `"sent"`
`[MPT]` | Marked post type enum.  One of the following strings: `"unmarked"` `"pinned"` `"collapsed"`

#### Response-only types
`[POST]` - A single post.
>```
>{
>   "id": [INT],
>   "threadId": [INT],
>   "parentId": [INT],
>   "author": [STR],
>   "category": [MOD],
>   "date": [DAT],
>   "body": [STR],
>   "lols":
>   [
>      {
>         "tag": [STR],
>         "count": [INT]
>      },
>      ...
>   ]
>}
>```

`[POSTS]` - A list of posts.
>```
>[
>   [POST*]
>]
>```

`[EVENT]` - A single event of any type.
>```
>{
>   "eventId": [INT],
>   "eventDate": [DAT],
>   "eventType": [E_TYPE],
>   "eventData": [E_DATA]  // check "type" first
>}
>```

`[EVENTS]` - A list of events.
>```
>[
>   [EVENT*]
>]
>```

`[E_TYPE]` - Event action type enum.  One of the following strings:
>- `"newPost"` - data will be `[E_NEWP]`
>- `"categoryChange"` - data will be `[E_CATC]`
>- `"lolCountsUpdate"` - data will be `[E_LOLS]`

`[E_DATA]` - Event-specific data.  Abstract base type which may be any one of the following concrete types:
>- `[E_NEWP]` - new post
>- `[E_CATC]` - category change
>- `[E_SMSG]` - server message
>- `[E_LOLS]` - tag counts update

`[E_NEWP]` - New post event data.
>```
>{
>   "postId": [INT],
>   "post": [POST],
>   "parentAuthor": [STR]
>}
>```

`[E_CATC]` - Category change event data.
>```
>{
>   "postId": [INT],
>   "category": [MODN]
>}
>```

`[E_SMSG]` - Server message event data.
>```
>{
>   "message": [STR]
>}
>```

`[E_LOLS]` - ShackLOL tag counts update.  Only tag counts that have changed are included.
>```
>{
>   "updates":
>   [
>      {
>         "postId": [INT],
>         "tag": [STR],
>         "count": [INT]
>      },
>      ...
>   ]
>}
>```

### Error Responses
If an API call results in an error, it is returned in the following JSON structure.
```
{
   "error": true,
   "code": [STR],
   "message": [STR]
}
```
The documentation for each API call lists which error codes are possible.  The following two error codes are possible on any API call, and are thus not listed on each individual call.  In both cases it is recommended that the client simply display the error message and then cancel whatever operation caused it.

HTTP status | Error code | Description
--- | --- | ---
500 | `ERR_SERVER` | Unexpected error.  Could be a communications failure, server outage, exception, etc.  The client did not do anything wrong.
400 | `ERR_ARGUMENT` | Invalid argument.  The client passed an argument value that violates a documented constraint.  The client contains a bug.

### Client Implementation Guide
These are general guidelines to follow when implementing a "full featured" client based on the v2 API.  Feel free to pick and choose based on your client's unique needs.  All of the API calls are designed to stand alone, as well as work in conjunction with the others.

At application startup:
- Call [/v2/clientData/](#client-data) methods to retrieve the user's client settings, if your client supports cloud synchronization of its settings.
- Call [/v2/getNewestEventId](#get-v2getnewesteventid) and save the event ID.  This ID will be continually updated as new events arrive.
- Call [/v2/getChatty](#get-v2getchatty) to bootstrap your local copy of the chatty, including all active threads.
- If your client shows lightning bolts for 10-year users, then call [/v2/getAllUserRegistrationDates](#get-v2getalluserregistrationdates) to bootstrap your list of registration dates.  If you encounter a username that isn't in your list, then call [/v2/getUserRegistrationDate](#get-v2getuserregistrationdate).  For some usernames (specifically, usernames containing punctuation characters), this may fail.

In a loop running until the application exits:
- *For desktop clients and other "unlimited energy/bandwidth/processor" scenarios:*   
Call [/v2/waitForEvent](#get-v2waitforevent), passing the last event ID (either from the original [/v2/getNewestEventId](#get-v2getnewesteventid) call, or the previous loop).  This will block until an event is ready, so your loop does not need any delays (unless you want to artificially limit the rate of events).
- *For mobile clients and other "limited energy/bandwidth/processor" scenarios:*   
Call [/v2/pollForEvent](#get-v2pollforevent), passing the last event ID (either from the original [/v2/getNewestEventId](#get-v2getnewesteventid) call, or the previous loop).  This will always return immediately, but may return zero events.  Then delay for length of time of your choosing (perhaps 1 minute) to allow your WiFi/3G/LTE radio to go idle.
- If `ERR_TOO_MANY_EVENTS` is returned, then throw out your copy of the chatty and start over by calling [/v2/getNewestEventId](#get-v2getnewesteventid) and [/v2/getChatty](#get-v2getchatty).  If the call fails with a different error, then display the error message and exit the loop rather than continuing to call it.

When your event loop retrieves a new event:
- For a new post, insert the post into your copy of the chatty.
- For a category change to "nuked", remove the post and all of its children from your copy of the chatty.
- For a category change to anything else, update the existing post in your copy of the chatty.  If the post does not exist in your copy of the chatty, then it must have been previously nuked and now has been reinstated.  Call [/v2/getThread](#get-v2getthread) to get the subthread rooted at this post, and insert all of the posts into your copy of the chatty.
- For a server message, show a message box to the user with the specified administrator message.

When the user changes a client option:
- If the username and password were changed, call [/v2/verifyCredentials](#post-v2verifycredentials) to ensure the login information is valid.
- Call [/v2/clientData/](#client-data) methods to save the updated client options.

## Threads
These API calls relate to the chatty itself.   These are the core of the v2 API.

### GET /v2/getChatty
Gets the list of recently bumped threads, starting with the most recently bumped.  Only "active" threads (i.e. threads that have not expired) are included.  Thus this essentially grabs the entire chatty.  The full threads are returned.  You should call this method to bootstrap your application's local copy of the chatty, and then use [/v2/waitForEvent](#get-v2waitforevent) to keep it up to date.

Parameters:
- None.

Response:
```
{
   "threads":
   [
      {
         "threadId": [INT],
         "posts": [POSTS]
      },
      ...  // one for each thread
   ]
}
```

### GET /v2/getChattyRootPosts
Gets one page of root posts, without any replies (to save space).  This is intended for use by mobile clients.

Parameters:
- `offset=[INT?]` - Number of threads to skip (for paging).  Default is 0.
- `limit=[INT?]` - Maximum number of threads to return (for paging).  Default is 40.
- `username=[STR?]` - If provided, this allows the isParticipant flag to be returned for each thread indicating whether the user posted that thread or replied to it.  If not provided, the isParticipant flag will always be false.

Response:
```
{
   "totalThreadCount": [INT],
   "rootPosts":
   [
      {
         "id": [INT],
         "date": [DAT],
         "author": [STR],
         "category": [MOD],
         "body": [STR],
         "postCount": [INT],  // count includes the root post
         "isParticipant": [BIT]
      },
      ...  // one for each thread
   ]
}
```

### GET /v2/getThread
Gets all of the posts in one or more threads.  If an invalid ID is passed (or if the ID of a nuked post is passed), then that thread will be silently omitted from the resulting list of threads.

Parameters:
- `id=[INT+,50]` - One or more IDs.  May be any post in the thread, not just the OP.

Response:
```
{
   "threads":
   [
      {
         "threadId": [INT],
         "posts": [POSTS]
      },
      ...  // one for each thread
   ]
}
```

### GET /v2/getThreadPostCount
Gets the number of posts in one or more threads, including the root post (i.e. the post count is always at least 1).

Parameters:
- `id=[INT+,200]` - One or more thread IDs.  May be any post in the thread, not just the OP.

Response:
```
{
   "threads":
   [
      {
         "threadId": [INT],
         "postCount": [INT]
      },
      ...  // one for each thread
   ]
}
```

## Posts
These API calls relate to the chatty itself.   These are the core of the v2 API.

### GET /v2/getNewestPostInfo
Gets the ID and date of the most recent post in the database.  If there are no posts in the database, then "id" is 0 and "date" will be present but has no meaning.

Parameters:
- None.

Response:
```
{
   "id": [INT],
   "date": [DAT]
}
```

### GET /v2/getPost
Gets one or more individual posts, specified by ID.

Parameters:
- `id=[INT+,50]` - The post IDs to retrieve.

Response:
```
{
   "posts": [POSTS]
}
```

### POST /v2/postComment
Posts a new comment.

Parameters:
- `username=[STR]` - Username.
- `password=[STR]` - Password.
- `parentId=[INT]` - The ID of the post we're replying to, or 0 for a new thread.
- `text=[STR]` - The body of the post.

Response:
```
{
   "result": "success"
}
```

Errors:
- `ERR_INVALID_LOGIN`
- `ERR_INVALID_PARENT`
- `ERR_POST_RATE_LIMIT`
- `ERR_BANNED`
- `ERR_NUKED`

### GET /v2/search
Performs a comment search.  At least one of [terms, author, parentAuthor, category] must be specified.

Parameters:
- `terms=[STR?]` - Search terms.
- `author=[STR?]` - Author.
- `parentAuthor=[STR?]` - Parent author.
- `category=[MOD?]` - Moderation flag.
- `offset=[INT?]` - Number of results to skip.  0 is the default, which gets the first page of results.
- `limit=[INT?,500]` - Maximum number of results to return.  35 is the default.  Larger limits may take a long time to retrieve.
- `oldestFirst=[BIT?]` - Whether to get results oldest first.  Default: false.

Response:
```
{
   "posts": [POSTS]
}
```

### POST /v2/requestReindex
**Deprecated.**  This call does nothing but return success.

Parameters:
- None.

Response
```
{
   "result": "success"
}
```

### POST /v2/setPostCategory
For moderators, sets the category (moderation flag) of a post.

Parameters:
- `username=[STR]` - Username.
- `password=[STR]` - Password.
- `postId=[INT]` - Post ID.
- `category=[MODN]` - Moderation flag (possibly nuked).

Response:
```
{
   "result": "success"
}
```

Errors:
- `ERR_INVALID_LOGIN`
- `ERR_NOT_MODERATOR`
- `ERR_INVALID_POST`

## Events
Events allow the server to inform the client of any changes that are made, which the client would need to know to keep its local copy of the chatty up to date.  The following list describes all of the event types:
- `"newPost"` – A new post has been added.
- `"categoryChange"` – The category of an existing post has been modified.
- `"lolCountsUpdate"` - LOL counts changed.

The category change event encompasses the following three things that may happen to a post after it is initially made:
- The post may be nuked (removed from the chatty).
- If the post was previously nuked, then it may be unnuked (reinstated in the chatty).
- The post may be flagged with a moderation category like "informative".

These are considered a change to the post's category.  To make this work, the standard set of categories (ontopic, nws, stupid, political, tangent, informative) is augmented with the special flag "nuked".  This gives us a nice way to represent nukes, unnukes, and flags the same way: as a change to the post category.

### GET /v2/getNewestEventId
Gets the most recent event in the database.

Parameters:
- None.

Response:
```
{
   "eventId": [INT]
}
```

### GET /v2/waitForEvent
Waits until a new event occurs, and then returns the information about all events that occurred since the last event seen by the client (as specified in the `lastEventId` argument).  This is the primary method by which the client's local copy of the world is kept up-to-date.  The client should process all events in sequential (by numeric ID) order.

A maximum of 10,000 events are returned.  An error is returned if more than 10,000 events have occurred since your specified `lastEventId`.  In that case, throw out your world and start over.  This will be faster than trying to catch up with a massive list of individual updates.

Note that sometimes this will return an empty list of events.  This is normal.  For instance, if no events have occurred yet.

Parameters:
- `lastEventId=[INT]` - Wait until any event newer than this ID appears.  If a newer event already exists, then the request returns immediately without waiting.

Response:
```
{
   "lastEventId": [INT],  // new lastEventId to be used in your next loop
   "events": [EVENTS]
}
```

Errors:
- `ERR_TOO_MANY_EVENTS`

### GET /v2/pollForEvent
Returns the information about all events (if any) that occurred since the last event seen by the client (as specified in the `lastEventId` argument).  This method is for use by clients in limited bandwidth or limited processor scenarios.  It is expected that these clients would call this method around once per minute (the interval is up to the developer's discretion).  Desktop clients (and phone clients who want a faster update rate at the expense of battery life) should use [/v2/waitForEvent](#get-v2waitforevent).  The client should process all events in sequential (by numeric ID) order.

A maximum of 10,000 events are returned.  An error is returned if more than 10,000 events have occurred since your specified `lastEventId`.  In that case, throw out your world and start over.  This will be faster than trying to catch up with a massive list of individual updates.

Parameters:
- `lastEventId=[INT]` - Return any event newer than this ID.

Response:
```
{
   "lastEventId": [INT],  // new lastEventId to be used in your next loop
   "events": [EVENTS]
}
```

Errors:
- `ERR_TOO_MANY_EVENTS`

## Users
These API calls pertain to user accounts.

### GET /v2/checkConnection
**Deprecated.** This backwards-compatibility stub always returns success.

Parameters:
- None.

Response:
```
{
   "result": "success"
}
```

### POST /v2/verifyCredentials
Checks the validity of the given username and password.

Parameters:
- `username=[STR]` - Username.
- `password=[STR]` - Password.

Response:
```
{
   "isValid": [BIT],
   "isModerator": [BIT]
}
```

### GET /v2/getAllTenYearUsers
**Deprecated.** This backwards-compatibility stub always returns an empty list.

Parameters:
- None.

Response:
```
{
   "users": []
}
```

## Messages

### POST /v2/getMessages
Gets a page of messages in the user’s inbox or sent mailbox. 

Parameters:
- `username=[STR]` - Username.
- `password=[STR]` - Password.
- `folder=[MBX]` - The mailbox folder.
- `page=[INT]` - 1-based page number.

Response:
```
{
   "page": [INT],
   "totalPages": [INT],
   "totalMessages": [INT],
   "messages":
   [
      {
         "id": [INT],
         "from": [STR],
         "to": [STR],
         "subject": [STR],
         "date": [DAT],
         "body": [STR],
         "unread": [BIT]
      },
      ...  // one for each message
   ]
}
```

Errors:
- `ERR_INVALID_LOGIN`

### POST /v2/getMessageCount
Gets the total number of messages in the user's inbox as well as the number of unread messages on the first page.

Parameters:
- `username=[STR]` - Username.
- `password=[STR]` - Password.

Response:
```
{
   "total": [INT],
   "unread": [INT],
}
```

Errors:
- `ERR_INVALID_LOGIN`

### POST /v2/sendMessage
Sends a message.

Parameters:
- `username=[STR]` - Username.
- `password=[STR]` - Password.
- `to=[STR]` - Message recipient's username.
- `subject=[STR]` - Subject line.
- `body=[STR]` - Post body.

Response:
```
{
   "result": "success"
}
```

Errors:
- `ERR_INVALID_LOGIN`
- `ERR_INVALID_RECIPIENT`

### POST /v2/markMessageRead
Marks a message as read.  If the message does not exist, then the method returns successfully without doing anything.

Parameters:
- `username=[STR]` - Username.
- `password=[STR]` - Password.
- `messageId=[INT]` - Message ID.

Response:
```
{
   "result": "success"
}
```

Errors:
- `ERR_INVALID_LOGIN`
- `ERR_INVALID_MESSAGE`

### POST /v2/deleteMessage
Deletes a message.  If the message does not exist, then the method returns successfully without doing anything.

Parameters:
- `username=[STR]` - Username.
- `password=[STR]` - Password.
- `messageId=[INT]` - Message ID.
- `folder=[MBX]` - Mailbox (inbox or sent).

Response:
```
{
   "result": "success"
}
```

Errors:
- `ERR_INVALID_LOGIN`
- `ERR_INVALID_MESSAGE`

## Client Data
The v2 API supports server storage ("cloud synchronization") of client data (primarily user preferences, but it's really just a general purpose store for the client's discretionary use).  There are two types of client data associated with each user:

- Shared client data is common to all clients.  For instance, the user's post filters (nws, political, etc.) are shared because every client supports this filtering feature.  These clients can support cloud synchronization of this preference by reading and writing this shared data.  All the shared data is available via formalized API methods with well-defined types and formats.
- Private client data is different for each client.  Here the client can store its own preferences and data which necessarily cannot be shared with other clients.  For instance, window positions, client-specific feature preferences, etc.  This data is available via generic string read/write methods.  It is recommended that you Base64-encode your data before passing it to this API.

Access to client data requires only a username.  Don't store secrets in the client data without encrypting it.  Changes to client data are logged by IP address so that troublemakers can be banned.  Troublemakers are also likely to be publicly shamed by this API's author.

### GET /v2/clientData/getCategoryFilters
Gets the user's moderation flag filters.  A value of true indicates that posts in that category are shown.

Parameters:
- `username=[STR,50]` - Username.

Response:
```
{
   "filters":
   {
      "nws": [BIT],
      "stupid": [BIT],
      "political": [BIT],
      "tangent": [BIT],
      "informative": [BIT]
   }
}
```

### POST /v2/clientData/setCategoryFilters
Sets the user's moderation flag filters.  A value of true indicates that posts in that category are shown.

Parameters:
- `username=[STR,50]` - Username.
- `nws=[BIT]` - Not work safe filter.
- `stupid=[BIT]` - Stupid filter.
- `political=[BIT]` - Political/religious filter.
- `tangent=[BIT]` - Tangent filter.
- `informative=[BIT]` - Informative filter.

Response:
```
{
   "result": "success"
}
```

### GET /v2/clientData/getMarkedPosts
Gets all the user's marked posts (pinned or collapsed).

Parameters:
- `username=[STR,50]` - Username.

Response:
```
{
   "markedPosts":
   [
      {
         id: [INT],
         type: [MPT]
      },
      ...  // one for each marked thread
   ]
}
```

### POST /v2/clientData/clearMarkedPosts
Clears the user's marked posts.

Parameters:
- `username=[STR,50]` - Username.

Response:
```
{
   "result": "success"
}
```

### POST /v2/clientData/markPost
Marks a post as unmarked, pinned, or collapsed.  The default for a regular post is unmarked.

Parameters:
- `username=[STR,50]` - Username.
- `postId=[INT]` - Post ID.
- `type=[MPT]` - Mark type.

Response:
```
{
   "result": "success"
}
```

Errors:
- `ERR_POST_DOES_NOT_EXIST`

### GET /v2/clientData/getClientData
Gets the client-specified data for the specified user.  This is just a blob of text that can be anything the client wants to store on the server (e.g. user preferences).  This data is specific to a particular client so that one client's data does not interfere with another client's data for the same user.

Parameters:
- `username=[STR,50]` - Username.
- `client=[STR,50]` - The unique name of this client.  This is chosen by the client author.  It is not displayed anywhere; it is only used to distinguish one client's data from another.  Recommended strings are something short and descriptive, without a version number.  Examples: "lamp", "chromeshack", etc.

Response:
```
{
   "data": [STR]
}
```

### POST /v2/clientData/setClientData
Sets the private client data for the specified user.  This is just a blob of text that can be anything the client wants to store on the server (e.g. user preferences).  This data is specific to a particular client so that one client's data does not interfere with another client's data for the same user.  Beware: anyone can access this data with just a username.  Do not store secret or private information without encrypting it.

Parameters:
- `username=[STR,50]` - Username.
- `client=[STR,50]` - The unique name of this client.  This is chosen by the client author.  It is not displayed anywhere; it is only used to distinguish one client's data from another.  Recommended strings are something short and descriptive, without a version number.  Examples: "lamp", "chromeshack", etc.
- `data=[STR,100000]` - Client-specified data.  I recommend Base64-encoding this data.  Maximum: 100,000 bytes.

Response
```
{
   "result": "success"
}
```

## Legacy v1 API
This API exists only to support [Latest Chatty](https://itunes.apple.com/us/app/latest-chatty/id287316743?mt=8) on iOS.  Do not use it in new applications.

### Data Types (v1)
The v1 comment data structure is hierarchical.  Each comment has a list of its children, and each of its children have lists of their children, and so forth.  The children list is empty for leaf nodes.

Type | Description
--- | ---
`[V1_DAT]` | Date and time in freeform text like "Aug 02, 2015 8:01pm PDT".  Note the Pacific time zone, rather than UTC.
`[V1_MOD]` | Moderation flag enum.  Same as `[MOD]` except it uses `"offtopic"` instead of `"tangent"`.  One of the following strings: `"ontopic"` `"nws"` `"stupid"` `"political"` `"offtopic"` `"informative"`

`[V1_COMMENT]` - A comment and its nested children.  This is a recursive data structure.
>```
>{
>   "comments": [V1_COMMENT*],
>   "reply_count": [INT], // includes itself, so it's always at least 1
>   "body": [STR],
>   "date": [V1_DAT],
>   "participants": [STR*], // list of usernames that posted in this thread
>   "category": [V1_MOD],
>   "last_reply_id": [STR], // ID of most recent reply converted to a string
>   "author": [STR],
>   "preview": [STR], // tags stripped out, spoilers replaced with underscores, truncated with ellipsis
>   "id": [STR] // post ID converted to a string
>}
>```

`[V1_PAGE]` - A page of comments.
>```
>{
>   "comments": [V1_COMMENT*],
>   "page": [STR], // 1-based page number converted to a string
>   "last_page": [INT],
>   "story_id": 0, 
>   "story_name": "Latest Chatty" 
>}
>```

`[V1_THREAD]` - A thread of comments.
>```
>{
>   "comments": [V1_COMMENT*],
>   "page": 1, 
>   "last_page": 1, 
>   "story_id": 0, 
>   "story_name": "Latest Chatty", 
>}
>```

`[V1_SEARCH_RESULT]` - A comment search result.
>```
>{
>   "comments": [],
>   "last_reply_id": null, 
>   "author": [STR],
>   "date": [V1_DAT],
>   "story_id": 0, 
>   "category": null, 
>   "reply_count": null, 
>   "id": [STR], // post ID converted to a string
>   "story_name": "", 
>   "preview": [STR], // tags stripped out, spoilers replaced with underscores, truncated with ellipsis
>   "body": null 
>}
>```

### Error Responses (v1)
Errors are reported using the following JSON structure.

```
{
   "faultCode": "AMFPHP_RUNTIME_ERROR", 
   "faultString": [STR], // error message
   "faultDetail": [STR] // source path and line
}
```

### GET /chatty/about
**Deprecated.** Gets an HTML page with information about the server.

### GET /chatty/index.json
**Deprecated.** Gets the first page of active threads.  This is the same as /chatty/0.1.json.

Response: `[V1_PAGE]`

### GET /chatty/`[INT]`.`[INT]`.json
**Deprecated.** Gets the Nth page of active threads, where N is the second number in the URL.  The first number is ignored.

Response: `[V1_PAGE]`

### GET /chatty/thread/`[INT]`.json
**Deprecated.** Gets a particular thread and its replies.  The number in the URL is the ID of some post in the thread (not necessarily the root post).

Response: `[V1_THREAD]`

### GET /chatty/search.json
**Deprecated.** Comment search.  At least one of the parameters must be specified.

Parameters:
- `terms=[STR?]` - Search terms
- `author=[STR?]` - Author
- `parent_author=[STR?]` - Parent author
- `page=[INT?]` - 1-based page number (defaults to 1)

Response:
```
{
   "terms": [STR],
   "author": [STR],
   "parent_author": [STR],
   "last_page": [INT],
   "comments": [V1_SEARCH_RESULT*]
}
```

### GET /chatty/stories.json
**Deprecated.** Gets the first page of front-page news articles.

Response:
```
[
   {
      "body": [STR], // same as preview
      "comment_count": [INT],
      "date": [V1_DAT],
      "id": [INT],
      "name": [STR],
      "preview": [STR],
      "url": [STR],
      "thread_id": ""
   },
   …
]
```

### GET /chatty/stories/`[INT]`.json
**Deprecated.** Gets the full story body.  The number in the URL is the story ID.

Response:
```
{
   "preview": [STR],
   "name": [STR],
   "body": [STR], // html
   "date": [V1_DAT],
   "comment_count": [INT], // not including the root post, so may be 0
   "id": [INT],
   "thread_id": [INT]
}
```

### GET /chatty/stories/`[INT]`.`[INT]`.json
**Deprecated.** Same as /chatty/stories/`[INT]`.json.  The second number in the URL is ignored.

### POST /chatty/messages.json
**Deprecated.** Gets the first page of the user's inbox.  Username and password are passed via HTTP basic authentication.

Response:
```
{
   "user": [STR],
   "messages": [
      {
         "id": [STR], // ID number converted to a string
         "from": [STR],
         "to": [STR],
         "subject": [STR],
         "date": [STR], // freeform date text like "August 1, 2015, 12:03 am"
         "body": [STR],
         "unread": [BIT]
      },
      …
   ]
}
```

Error: (plain text, not JSON)
```
error_get_failed
```

### POST /chatty/messages/`[INT]`.json
**Deprecated.** Marks a message as read.  Username and password are passed via HTTP basic authentication.  The number in the URL is the message ID to mark as read.

Response: (plain text, not JSON)
```
ok
```

Error: (plain text, not JSON)
```
error_mark_failed
```

### POST /chatty/messages/send/
**Deprecated.** Sends a message.  Username and password are passed via HTTP basic authentication.

Parameters:
- `to=[STR]` - Recipient username
- `subject=[STR]` - Message subject
- `body=[STR]` - Message body

Response: (plain text, not JSON)
```
OK
```

Error: (plain text, not JSON)
```
error_send_failed
```

### POST /chatty/post/
**Deprecated.** Posts a comment.  Username and password are passed via HTTP basic authentication.

Parameters:
- `parent_id=[STR]` - Parent ID.  May be 0 or "" to post a root thread.
- `body=[STR]` - Comment text.

Response: (blank)

Errors: (plain text, not JSON)
- `error_login_failed`
- `error_post_rate_limit`
- `error_post_failed`

