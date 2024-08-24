namespace UniProtProvider.DesignTime
open ProviderImplementation.ProvidedTypes
open UniProtProvider.RunTime
open Client.UniProtClient
open Types

module internal InnerTypes =
    let mutable count = 0
    let nextNumber() = count <- count + 1; count // serves to generate unique type names

    let getProperties (props : array<ProtIncomplete>) () =
        [for i in props do
            let name = System.String.Concat(i.proteinDescription.recommendedName.Value.fullName.value, " (", i.uniProtkbId, ")")
            let value = i.uniProtkbId
            let p =
                ProvidedProperty(propertyName = name,
                propertyType = typeof<Prot>,
                getterCode = (fun args -> <@@ getProteinById value @@>))
            p]


    let rec addByOrganism (param : Params) (outerType: ProvidedTypeDefinition) () = 
        let byOrganism = ProvidedMethod("ByOrganism", [], typeof<obj>)
        byOrganism.DefineStaticParameters([ProvidedStaticParameter("Name", typeof<string>)], fun methName args ->
            let name = args.[0] :?> string
            param.organism <- name

            let t = ProvidedTypeDefinition("InnerType" + string(nextNumber()), Some typeof<obj>, true)
            let result = getProteinsByKeyWord param
            t.AddMembersDelayed(getProperties result.results)
            t.AddMemberDelayed(addByOrganism param t)

            let cursor = getCursor param |> Async.RunSynchronously
            if cursor.IsSome then
                let nextParam = param.Clone() in nextParam.cursor <- cursor.Value
                t.AddMemberDelayed(addNext nextParam t)
            outerType.AddMember(t)

            let m = ProvidedMethod(methName, [], t, invokeCode = fun _ -> <@@ obj() @@>)
            outerType.AddMember(m)
            m
        )
        byOrganism

    and addNext (param: Params) (outerType: ProvidedTypeDefinition) () =

        let result = getProteinsByKeyWord param
        let next = 
            ProvidedTypeDefinition("InnerType" + string(nextNumber()),
            Some typeof<obj>,
            hideObjectMethods=true)

        next.AddMember(ProvidedConstructor([], fun _ -> <@@ obj() @@>))
        next.AddMembersDelayed (getProperties result.results)
        next.AddMemberDelayed (addByOrganism param next)

        outerType.AddMember next

        let cursor = getCursor param |> Async.RunSynchronously
        if cursor.IsSome then
            let nextParam = param.Clone() in nextParam.cursor <- cursor.Value
            next.AddMemberDelayed(addNext nextParam next)

        let p =
            ProvidedProperty(propertyName="More...",
            propertyType = next,
            getterCode = (fun _ -> <@@ obj() @@>))
        p

    and  byTaxonomy () = 
        let byTaxonomy = ProvidedMethod("ByTaxonName", [], typeof<obj>)
        byTaxonomy

    let addSuggestions (sug : array<Suggestion>) (outerType: ProvidedTypeDefinition) =
        for i in sug do
            let keyword = i.query.Value
            let param = Params(keyword)
            let result = getProteinsByKeyWord param
            let suggested = 
                ProvidedTypeDefinition("InnerType" + string(nextNumber()),
                Some typeof<obj>,
                hideObjectMethods=true)

            suggested.AddMember(ProvidedConstructor([], fun _ -> <@@ obj() @@>))
            suggested.AddMembersDelayed (getProperties result.results)

            let param = Params(keyword)
            suggested.AddMemberDelayed(addByOrganism param suggested)

            let cursor = getCursor param |> Async.RunSynchronously
            if cursor.IsSome then
                let nextParam = param.Clone() in nextParam.cursor <- cursor.Value
                suggested.AddMemberDelayed(addNext nextParam suggested)

            outerType.AddMember suggested
            let p =
                ProvidedProperty(propertyName = keyword,
                propertyType = suggested,
                getterCode = (fun _ -> <@@ obj() @@>))
            outerType.AddMember p