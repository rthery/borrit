const spreadsheetId = SpreadsheetApp.getActiveSpreadsheet().getId();

function doPost(request) 
{
  var command = JSON.parse(request.postData.contents);
  let lock = LockService.getScriptLock();
  lock.waitLock(6000);

  if(command.Operation == "get")
  {
    let cache = CacheService.getScriptCache();
    let data = cache.get("borrit");
    if (data == null) 
    {
      data = loadRows();
      updateCache(data);
    }
    else 
    {
      data = JSON.parse(data);
    }
    return buildResponse({Success: true, Error: "", BorrowedEntries: data }, lock);
  }
  if(command.Operation == "borrow")
  {
    if(command.BorrowedEntries == undefined)
    {
      return buildResponse({Success: false, Error: "missing borrowed entries"}, lock);
    }
    borrowAssets(command.BorrowedEntries);
    updateCache(loadRows());
    return buildResponse({Success: true, Error: ""}, lock);
  }
  if(command.Operation == "return")
  {
    if(command.ReturnedGuids == undefined)
    {
      return buildResponse({Success: false, Error: "missing retruned guids"}, lock);
    }
    returnAssets(command.ReturnedGuids);
    updateCache(loadRows());
    return buildResponse({Success: true, Error: ""}, lock);
  }

  return buildResponse({Success: false, Error: "operation unknown"}, lock);
}

function loadRows()
{
  SpreadsheetApp.flush();
  var sheet = SpreadsheetApp.openById(spreadsheetId).getSheets()[0];
  let retVal = [];
  const data = sheet.getDataRange().getValues();
  for(let i = 1; i < data.length; ++i)
  {
    const row = data[i];
    retVal.push({ Guid: row[0], User: row[1], UtcDateTime: row[2] });
  }
  return retVal;
}

function updateCache(data)
{
  let cache = CacheService.getScriptCache();
  cache.remove("borrit");
  cache.put('borrit', JSON.stringify(data), 21600); // cache for 6 hours(is the max), max 100k data
}

function borrowAssets(borrowedEntries)
{
  var sheet = SpreadsheetApp.openById(spreadsheetId).getSheets()[0];
  for(let i = 0; i < borrowedEntries.length; ++i)
  {
    const entry = borrowedEntries[i];
    const cell = sheet.createTextFinder(entry.Guid).matchEntireCell(true).findNext();
    if(cell != null)
    {
      sheet.deleteRow(cell.getRow());
    }
    sheet.appendRow([entry.Guid, entry.User, entry.UtcDateTime]);
  }
}

function returnAssets(guids)
{
  var sheet = SpreadsheetApp.openById(spreadsheetId).getSheets()[0];
  for(let i = 0; i < guids.length; ++i)
  {
    const entry = guids[i];
    const cell = sheet.createTextFinder(entry).matchEntireCell(true).findNext();
    if(cell != null)
    {
      sheet.deleteRow(cell.getRow());
    }
  }
}

function buildResponse(obj, lock)
{
  lock.releaseLock();
  return ContentService.createTextOutput(JSON.stringify(obj));
}

// Run this method if you modify the spreadsheet data manually
function syncCacheWithSheet()
{
  updateCache(loadRows());
}

function clearCache()
{
  let cache = CacheService.getScriptCache();
  cache.remove("borrit");
}
