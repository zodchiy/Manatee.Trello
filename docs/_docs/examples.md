---
title: Examples
category:
order: 8
---

This page contains some examples of using this library.

For all of these examples, `factory` is a local variable of type `ITrelloFactory` that holds an instance of `TrelloFactory`.  These examples also assume that the default authentication has been set up.

## Getting all of the boards for the owner of the token

```csharp
// TrelloFactory.Me() is one of two awaitable factory methods.  This is to get the member ID.
// Entities created from other methods will need to be refreshed before data can be accessed.
var me = await factory.Me();

foreach(var board in me.Boards)
{
    Console.WriteLine(board); // prints board.Name
}
```

## Getting a known card and updating some of its data

```csharp
var card = factory.Card("[some known ID]");
await card.Refresh();

// basic fields
card.Name = "a new name";
card.Description = "hello";
card.Postion = Position.Top;

// custom fields
var checkBoxField = card.CustomFields.FirstOrDefault(f => f.Type == CustomFieldTypes.CheckBox);
if (checkBoxField.Value)
{
    var numericCustomField = card.CustomFields.FirstOrDefault(f => f.Type == CustomFieldType.Number);
    numericCustomField.Value = 9;
}

// Drop down fields only allow a predetermined set of values.  These values can be found
// in the field's definition, accessible through the board.  While the definition is
// accessible through the field, Trello doesn't send the options during this call.  To
// ensure that the drop down field options are available, it's a good idea to refresh the
// definition before accessing the field itself.  Once this is done, the definition will
// be cached and the options will be available through the field's Definition property.
await card.Board.CustomFieldDefinitions.Refresh();
var dropDownField = card.CustomFields.FirstOrDefault(f => f.Type == CustomFieldType.DropDown);
dropDownField.Value = dropDownField.Definition.Options[2];

// Trello doesn't return a custom field for a card if it doesn't have a value for that field.
// So to set a custom field value on a card that has no value for that field, you have to
// get the field's definition first and call the appropriate setter method.  All setter methods
// are available on all custom field definitions, so be sure you set the right type of data
// for the right type of field.
var textFieldDefinition = card.Board.CustomFieldsDefinitions.FirstOrDefault(f => f.Type == CustomFieldType.Text);
var textField = await textFieldDefinition.SetValueForCard(card, "some text");
```

The above code will consolidate the *basic fields* changes and make a single call to set those properties.  Since the custom fields are different entities, two additional calls will be made to set the respective values.

## Changing the data that's downloaded

All of an entity's data and most of its child data is downloaded when refreshed.  For boards, this can include lists, cards, members, etc.  The data that is downloaded is managed by the `DownloadedFields` static property on the entity class.

```csharp
// We just want members (the users), but not memberships (permissions on the board),
// but the default is to get the memberships, not the members.  (Memberships include
// the member, but this is example code, so...)
Board.DownloadedFields &= ~Board.Fields.Memberships;
Board.DownloadedFields |= Board.Fields.Members;

var board = factory.Board("[some known ID]");
await board.Refresh();

// board.Memberships is empty, but board.Members has data!
foreach(var member in board.Members)
{
    Console.WriteLine(member); // prints member.FullName
}
```

## Adding a card to a list

### by getting the list from the board

```csharp
var board = factory.Board();
// We don't care about the board; we just want the lists.  By just refreshing
// the list collection, we reduce the amount of data that must be retrieved.
await board.Lists().Refresh();

// Refreshing the list collection downloaded all of the data for the lists as well.
var backlog = board.Lists.FirstOrDefault(l => l.Name == "Backlog");
var newCard = await backlog.Cards.Add("a new card");
// The card is returned with all of its data, so it doesn't need to be refreshed.
```

### by getting the list using its ID

```csharp
var backlog = factory.List("[some known ID]");
// To add a card, we don't need the list's data; no refresh needed.
var newCard = await backlog.Cards.Add("a new card");
// The card is returned with all of its data, so it doesn't need to be refreshed.
```

## Deleting a check item

```csharp
var card = factory.Card("[some known ID]");
// Check items are a special case for refreshing in that they download when refreshing
// the card.  No other collections download two levels deep like this.
await card.Refresh();
var checkList = card.CheckLists[0];
// Many collections allow indexing by a string.  The properties that this matches
// on varies by collection.  Here's we're indexing by the check item's name.
var checkItem = checkList.CheckItems["Bake cookies"];

await checkItem.Delete();
```

## Creating a webhook

```csharp
var card = factory.Card("[some known ID]");
// ITrelloFactory.Webhook<T>() is the other awaitable factory method.  This is
// because a call is made to create a new webhook.  The webhook data is downloaded,
// so no refresh call is required.
var webhook = await factory.Webhook(card, "http://post.back/url");
```

## Processing a webhook notification

This code would be placed inside a ASP.Net controller's POST method.  The JSON content would need to be read as a string and passed to `TrelloProcessor.ProcessNotification()` as shown below.

```csharp
var content = await Request.ReadAsStringAsync();
TrelloProcessor.ProcessNotification(content);
```

The processor will update the entity *if it exists in the cache*.  If the entity does not exist in the cache, no processing will occur.