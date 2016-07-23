namespace Rezoom
open System.Collections.Generic

type private Response = obj
type private DataSource = obj
type private Identity = obj

type ResponseCache() =
    let nullDataSource = new Dictionary<Identity, Response>()
    let byDataSource = new Dictionary<DataSource, Dictionary<Identity, Response>>()

    member __.Invalidate(dataSource : DataSource) =
        if isNull dataSource then nullDataSource.Clear()
        else ignore <| byDataSource.Remove(dataSource)

    member __.Store(dataSource : DataSource, identity : Identity, value : Response) =
        if isNull dataSource then
            nullDataSource.[identity] <- value
        else
            let mutable subCache : Dictionary<Identity, Response> = null
            if not <| byDataSource.TryGetValue(dataSource, &subCache) then
                subCache <- new _()
                byDataSource.[dataSource] <- subCache
            subCache.[identity] <- value

    member __.TryGetValue(dataSource : DataSource, identity : Identity, value : Response byref) =
        if isNull dataSource then
            nullDataSource.TryGetValue(identity, &value)
        else
            let mutable subCache : Dictionary<Identity, Response> = null
            if byDataSource.TryGetValue(dataSource, &subCache) then
                subCache.TryGetValue(identity, &value)
            else false