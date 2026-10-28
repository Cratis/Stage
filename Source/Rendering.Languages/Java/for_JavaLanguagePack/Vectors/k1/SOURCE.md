<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Frozen K1 Java vectors

These are lexical, not semantic-renderer, vectors from the Cratis/Stage #791 spike.
The Axon oracle is frozen at **fe14881ab82d2a73698eba0a06cb9ac9b2869662**
(Cratis/Stage-Axon, MIT license). The spike's Screenplay input snapshot is
52af009f4. Source paths below are relative to the #791 spike root:
`Stage/.ai-work/791-spike/`, containing `oracle/`, `oracle-reactions/`,
and `round2/resolved-baseline/diff.json`. No live checkout or oracle was modified.
The rendered Java retains its original MIT headers.

## Coverage and format

All **289** selected lexical files are included: 85 commands,
85 handlers, 117 events, and 2 reactions.
The compact plan is **288012 bytes**, below 400 KiB,
so no application-subset fallback is used. `digests.json` pins one UTF-8 SHA-256
per fully resolved file. The eleven complete Java goldens contain five real
corpus files and six supplemental synthetic cases, explicitly identified by
`case-`: case-only allocation collisions, nested-type shadowing, inherited
member-type shadowing, an obscured package requiring an outer import, and
unknown ancestry/omitted owner qualification. The synthetic cases are built
by `K1GoldenCases`, not attributed to the Axon oracle.

`plan.json` uses an indexed type table: each entry is
`[package, topLevelName, nestedNames]`. Each file records its stable source
identity, package, kind, and owner (unused by the declarative loader). Segment
arrays are `[t, literal]`, `[i]`, or `[d|n|r, typeIndex]`. `K1Fixture` maps them to
`DeclarativeLanguageFile`s: `t` to text, `i` to Imports, `d` to Declaration, `r` to
Reference. Every `n` (114) repeats the declaration of the file's own top-level
type (a constructor); the frozen plan has no nested types, so `n` maps to
Declaration. The literal `package <pkg>;\n` that ends the text before `[i]` is
removed, as is one leading `\n` of the text after it; the pack writes both.

**Test facts.** The pack needs facts for every type the plan does not declare.
`K1Fixture.Facts()` supplies them as empty member sets: the 181 types outside
the plan packages plus the 164 same-package siblings that the frozen 289-file
selection omits (for example `MoveOrderStream`). These are test facts, not
bundled facts; real member sets are unknown and the K1 sources do not need them.
All 289 sources contain exactly one top-level declaration and no explicit
extends or implements clauses.

The #791 lexical lift has no native tokens; its literal spans are retained.

## Regenerate offline

1. Recreate the #791 oracle and oracle-reactions from the frozen Axon revision
   and Screenplay input snapshot above, preserving the source paths below.
   Use the exact 289 selections from the spike's round-two diff.
2. Run the deliberately uncommitted offline tool
   `.ai-work/796/k1-converter/convert.mjs` from the Stage worktree. It rebuilds
   the lexical lift using the frozen source universe and java@1's JDK 21 list,
   preserves declaration names/constructors and static-member suffixes, removes
   only import spans for resolution, and interns structured type identities.
   It must report **289/289 exact original-spelling reconstructions** and
   **289/289 fully-qualified-reference canonical matches**. Canonicalization
   removes imports and normalizes CRLF; no statements or reaction bodies are
   replaced by gap placeholders. Original source hashes must match this table.
3. Digests and goldens are re-baselined from the declarative `JavaLanguagePack`
   (S6): the frozen corpus has 2,816 references; 166 are fully qualified (every
   one a reference to a plan record whose own annotation string repeats the
   record name) and 117 explicit `java.lang.*` imports were added. See the
   golden review in the working notes. The 166 qualified spellings are a known conservative result: the pack cannot prove a referenced plan type is final, so its own simple name (seen as a word, here inside an annotation string) stays in the closure and the reference is written qualified. To regenerate, resolve `K1Fixture.Files()`
   with `K1Fixture.Facts()`, hash each file's UTF-8 text with SHA-256 and rewrite
   `digests.json` and the goldens named in `goldens.json`.
4. Run `publish.mjs` to copy only the compact plan, digests, golden index,
   Java goldens, and this source description into this directory. Do not ship
   the offline converter or depend on the live oracle in specs. Run the Debug
   Rendering.Languages specs, including file/external ordering and unrelated
   file-addition invariance. No javac/runtime conformance is claimed here.

## Original source SHA-256

| Source file | SHA-256 |
| --- | --- |
| oracle-reactions/orders-reactions/src/main/java/sampleordersreactions/orders/events/AcknowledgementNoted.java | db5a8fbac2f57a84bf7df84184b9f67eedf7df57a35967df82ac3cae008304dd |
| oracle-reactions/orders-reactions/src/main/java/sampleordersreactions/orders/events/EventTags.java | 13f701f5ea1888c73b7830f6d7be78dfc69f937bc10603033d4e865ad63b1bd8 |
| oracle-reactions/orders-reactions/src/main/java/sampleordersreactions/orders/events/OrderAcknowledged.java | 248c1ae291b92c84dd2eb758dc29aca571a4812f3f5679a12d3f238dacf5943b |
| oracle-reactions/orders-reactions/src/main/java/sampleordersreactions/orders/events/OrderMoved.java | 814a0613fbccf6f8176236946d7d97bf2939cc0bd4419ea27e2b59567c490626 |
| oracle-reactions/orders-reactions/src/main/java/sampleordersreactions/orders/events/OrderPlaced.java | 4d461fcfdfb90be80fc31111ec7932b237066dd40ffc670ced8e3924763043e8 |
| oracle-reactions/orders-reactions/src/main/java/sampleordersreactions/orders/fulfilment/acknowledgeneworders/AcknowledgeNewOrdersReaction.java | 48090677dec835b3a18fe7e64c055a9205bafd3b3716eac1699459f3c8d9a482 |
| oracle-reactions/orders-reactions/src/main/java/sampleordersreactions/orders/fulfilment/acknowledgeorder/AcknowledgeOrder.java | a7db4ad33115754d49ae919ab41cf1949d2c923242437d721def1b1eb200bd72 |
| oracle-reactions/orders-reactions/src/main/java/sampleordersreactions/orders/fulfilment/acknowledgeorder/AcknowledgeOrderHandler.java | 30fd2a84b52f9803538572261c9eb39d502471cd1e24450aa36a3f24b940c39b |
| oracle-reactions/orders-reactions/src/main/java/sampleordersreactions/orders/fulfilment/moveorder/MoveOrder.java | e4790d2eb49fde6cbb4dced0c25183e65ce6792a97ccf3678c298a28a5ce2a29 |
| oracle-reactions/orders-reactions/src/main/java/sampleordersreactions/orders/fulfilment/moveorder/MoveOrderHandler.java | a30649b6bf4e933c47abddb3d762e80864bf295096b0f2d7f811bf39bd922b00 |
| oracle-reactions/orders-reactions/src/main/java/sampleordersreactions/orders/fulfilment/noteacknowledgements/NoteAcknowledgementsReaction.java | 513059d2eaa0451fe5dfc427eb23a8478eba238563cb285926ed57ed7ccf78fb |
| oracle-reactions/orders-reactions/src/main/java/sampleordersreactions/orders/orders/placeorder/PlaceOrder.java | 9cfcc503767bd5bdecb2aacdf0eac4d183e274a9fdf45c55bd17681edc23f7a2 |
| oracle-reactions/orders-reactions/src/main/java/sampleordersreactions/orders/orders/placeorder/PlaceOrderHandler.java | f32ba27d683fb67ea06c0d62ac39bfbe7cf0483d737b3fd9e1d6ef7db6373f5d |
| oracle/administration/src/main/java/corpusadministration/admin/audit/recordauditentry/RecordAuditEntry.java | f2f1ab406303533a905e16d0e787e8cb68e0d73b31b28f509c35e92178b53f66 |
| oracle/administration/src/main/java/corpusadministration/admin/audit/recordauditentry/RecordAuditEntryHandler.java | 81a0d572e18fb0c5f994ade33b279f384dccee3c80b79ae5f05b285fb5e17747 |
| oracle/administration/src/main/java/corpusadministration/admin/events/AuditEntryRecorded.java | c1734244b685efa09712814003143974244fede0e9e028f214708b5cc288ed07 |
| oracle/administration/src/main/java/corpusadministration/admin/events/EventTags.java | f00f35947e499eaacb51dd5e6ed3323712e2efecb8a97363b77d5e7bb679678b |
| oracle/administration/src/main/java/corpusadministration/admin/events/RoleRegistered.java | f9099bd56d0625cadeb7aae6a47ba1d88a8cc4c018fdb0faf24dcf91b5c4b559 |
| oracle/administration/src/main/java/corpusadministration/admin/events/SettingsSaved.java | 771b0e2f735c2fa33c50d7ebcd901e5cdb2e9e9a48b5235d217aed955edf6460 |
| oracle/administration/src/main/java/corpusadministration/admin/events/UserInvited.java | df9d6f1eba99e5ca47165143c74472b0deea89f6d7a9efb0d905c54b81687613 |
| oracle/administration/src/main/java/corpusadministration/admin/events/UserRoleChanged.java | f66eb62bcfbefed0b5dbeca725e655146c685f71ab2f660f50b4a1c6aa549b39 |
| oracle/administration/src/main/java/corpusadministration/admin/roles/registerrole/RegisterRole.java | e1c94075ace06bc276e924ed1ef56eee520bce00166b66fe3b68dd5aaa953390 |
| oracle/administration/src/main/java/corpusadministration/admin/roles/registerrole/RegisterRoleHandler.java | 5506e74931bbae7956236368a5bdcc6b367d96c6b299e2d963fdde972e7846c0 |
| oracle/administration/src/main/java/corpusadministration/admin/settings/savesettings/SaveSettings.java | 186d7ced2c7d9ce943a76bab8b43a007d033aab21669ac855a82312c518e2ea0 |
| oracle/administration/src/main/java/corpusadministration/admin/settings/savesettings/SaveSettingsHandler.java | 7304f9451e28d12a9e2ee6e8c4e9bdbf8bd6c1c58e718d4173a84dcd7f343aba |
| oracle/administration/src/main/java/corpusadministration/admin/users/changeuserrole/ChangeUserRole.java | 74e1acde6ff78154d2f6c8a6a5b26e0501cd7a9b208689970d4dab2bb8ac331b |
| oracle/administration/src/main/java/corpusadministration/admin/users/changeuserrole/ChangeUserRoleHandler.java | 72199507060bc1f323ff3b6b9e9ca958a6eade4add75ef4e7defb4095a2e00e0 |
| oracle/administration/src/main/java/corpusadministration/admin/users/inviteuser/InviteUser.java | e7557f3fbb25a6c0bd8ae74e7997ed70d699853201cb929837e5dcf7ca14d4ea |
| oracle/administration/src/main/java/corpusadministration/admin/users/inviteuser/InviteUserHandler.java | 7354d3b28330e2767598c05a01b22c8082a63b3e76d313040d84e4a13e6dafa2 |
| oracle/approvals/src/main/java/corpusapprovals/approvals/events/EventTags.java | 2b88f8e27602df8a8e7874553b998e7e45d64af82395e1a3e6ade75041ef1e11 |
| oracle/approvals/src/main/java/corpusapprovals/approvals/events/RequestDecided.java | 120467466a12d5c0128f65e21f766b87164f62368353655c66b6aa584c6a2196 |
| oracle/approvals/src/main/java/corpusapprovals/approvals/events/RequestSubmitted.java | 96a3bae9396e63419744e798e10aaf646b1fdfb469650e20556f8902f984c779 |
| oracle/approvals/src/main/java/corpusapprovals/approvals/events/RequestTypeDefined.java | 384395ba2ff2aa7d56a3c1f35005e1bbebf8cdad04ee65c15787fcee32a3f87d |
| oracle/approvals/src/main/java/corpusapprovals/approvals/inbox/deciderequest/DecideRequest.java | ac0195600a794aff4bc4540dd67f11742031b2a27b80e45e9a80ee02a7a7911f |
| oracle/approvals/src/main/java/corpusapprovals/approvals/inbox/deciderequest/DecideRequestHandler.java | e0a79000847e746a6bed674b128a2de6054e93dae42cbded39595beedfd67004 |
| oracle/approvals/src/main/java/corpusapprovals/approvals/requests/submitrequest/SubmitRequest.java | 33d92da4b61352a746afddd1191cfde39f176c2add8f3b65013755e8976ccc6a |
| oracle/approvals/src/main/java/corpusapprovals/approvals/requests/submitrequest/SubmitRequestHandler.java | 1b351625ecb352d0a949a0ddc5c59634586ccfe71cbeb3db1fb892a4c1e3fec6 |
| oracle/approvals/src/main/java/corpusapprovals/approvals/templates/definerequesttype/DefineRequestType.java | 6947831f8330dd3791a330cd52118b5ea17ca2eb6583d89c1359211c79c52832 |
| oracle/approvals/src/main/java/corpusapprovals/approvals/templates/definerequesttype/DefineRequestTypeHandler.java | 3d0651a845f899e1408d5597acf78781dffc7fc07602cf27c7fec1f0962a772c |
| oracle/bookings/src/main/java/corpusbookings/bookings/bookings/registerbooking/RegisterBooking.java | dcb7ba0da7af86d801021712432e49c5a65ec456fe8d7658e28ddeb8994b66dd |
| oracle/bookings/src/main/java/corpusbookings/bookings/bookings/registerbooking/RegisterBookingHandler.java | c8aff45ffc0eccbcf74feacf9f83ed5e6ba421dbc6263e2ea153d305cf656c44 |
| oracle/bookings/src/main/java/corpusbookings/bookings/events/BookingRegistered.java | 8d9c2c33464b17200a41a152e749b062b125a5bbe78014b4f9427a16847a6316 |
| oracle/bookings/src/main/java/corpusbookings/bookings/events/BookingRequested.java | ca4599e27e54bb2779e73f29a53f9b71d7beeb6096f2e3420f255997b0cb7e2d |
| oracle/bookings/src/main/java/corpusbookings/bookings/events/EventTags.java | 73b000d62b680eef6e1a68831ddd2af9492cc3a5d2bbf16863297500bfe9dd38 |
| oracle/bookings/src/main/java/corpusbookings/bookings/events/ResourceRegistered.java | 837f9f804bd4f9cbd17f2aa66e4ff9573392ea08ef2287db6a0779fe7b21ce34 |
| oracle/bookings/src/main/java/corpusbookings/bookings/newbooking/requestbooking/RequestBooking.java | 1e44505aa088c8a0564ef59d552dbaece08e504b31a3359ea8ae0e97c0249164 |
| oracle/bookings/src/main/java/corpusbookings/bookings/newbooking/requestbooking/RequestBookingHandler.java | 5cedc510f4dd6322a835d52d00dc24cb6c9341f685fac9a41f67f18ad87fe19e |
| oracle/bookings/src/main/java/corpusbookings/bookings/resources/registerresource/RegisterResource.java | 8b7cc7f9b74212e1c5446d68c6def83b2a44608e487e568aee087004df02c143 |
| oracle/bookings/src/main/java/corpusbookings/bookings/resources/registerresource/RegisterResourceHandler.java | eedd7ae043abeb413c0179044686853b8b952894cb86942fa3ed25e207ac2040 |
| oracle/catalog/src/main/java/corpuscatalog/catalog/categories/registercategory/RegisterCategory.java | 4bee56a0597d11f7ba3087cc48c5237e530a477cb641ba62bb3d982d9d791519 |
| oracle/catalog/src/main/java/corpuscatalog/catalog/categories/registercategory/RegisterCategoryHandler.java | 90c39aac867a425c585831f9f5ed0e54d01e77cf4787a304f7692f766bf65a90 |
| oracle/catalog/src/main/java/corpuscatalog/catalog/events/CategoryRegistered.java | ec8546213dcbcb127d0240864957cf77bb987affb519bb9669406f63fe5a6da6 |
| oracle/catalog/src/main/java/corpuscatalog/catalog/events/EventTags.java | e9a525af70732b83bfeb39470419de48d5f4ba4fc6cd643e41292f578fa0eea2 |
| oracle/catalog/src/main/java/corpuscatalog/catalog/events/ProductRegistered.java | 5e7db10b6cef4249f77bc5096d2e5ee2fd2cca37e8ec845ae6df34b16e619380 |
| oracle/catalog/src/main/java/corpuscatalog/catalog/products/registerproduct/RegisterProduct.java | 4a04c6fd6bef205d6c520a1a5cac1cbb8d07d95417c254872c78da82ee068586 |
| oracle/catalog/src/main/java/corpuscatalog/catalog/products/registerproduct/RegisterProductHandler.java | e5d8a07c71ed436532a79b58558d0085b09b924bd2c54fcafe16937c6d1902e2 |
| oracle/contracts/src/main/java/corpuscontracts/contracts/contracts/registercontract/RegisterContract.java | a3a425a8ddbaaea36fe788dadf1063a19fa519a0df830b8d5a72956cd8debb22 |
| oracle/contracts/src/main/java/corpuscontracts/contracts/contracts/registercontract/RegisterContractHandler.java | 698568440374a3f4257e30572e6a04e0f862cd5696dbd778140ed584800d7aee |
| oracle/contracts/src/main/java/corpuscontracts/contracts/events/ContractRegistered.java | 2279d56dddb2d9c4c4d869cbd1a9b801da3331b10137eb824761d3a8544d76fc |
| oracle/contracts/src/main/java/corpuscontracts/contracts/events/EventTags.java | 0c264a7cf502f3769477aa14a3131901d64c9919ebde1b33d6ba8db7b7382c13 |
| oracle/crm/src/main/java/corpuscrm/crm/activities/registeractivity/RegisterActivity.java | b0ab6642b71cd8e1923039024c0829064402629c4cd93f143cf22a71930784f7 |
| oracle/crm/src/main/java/corpuscrm/crm/activities/registeractivity/RegisterActivityHandler.java | 9acfb583e23942e9a467df6b3408f860d6a685f5e11fc290ee2c9ff4113e611b |
| oracle/crm/src/main/java/corpuscrm/crm/activities/updateactivity/UpdateActivity.java | d99c77602e730be7ab1186af19b2f5988364be296c2fc56ba6b911857b6f67e4 |
| oracle/crm/src/main/java/corpuscrm/crm/activities/updateactivity/UpdateActivityHandler.java | a33b5ce0f45fa3a3e77a5d41ba9fa08147ac987a00e7aaf967919b56cf143f5d |
| oracle/crm/src/main/java/corpuscrm/crm/companies/registercompany/RegisterCompany.java | 3e9270512e3745784aa6febbc7e23c008d5564eaac6307b405ad3e7a0930cdc2 |
| oracle/crm/src/main/java/corpuscrm/crm/companies/registercompany/RegisterCompanyHandler.java | 162ea03a4c185674f82b3dcdbd9d3fb81c8e75fd2b9a9f8723fa594d489bb082 |
| oracle/crm/src/main/java/corpuscrm/crm/companies/updatecompany/UpdateCompany.java | ffded006417985946dfd0badae6c3e5aced0890173dce2d652fcabe8762f1a62 |
| oracle/crm/src/main/java/corpuscrm/crm/companies/updatecompany/UpdateCompanyHandler.java | ae139bf31f3314670e0f525c6d1f336e05431d78e0b712d15cf35a5876c63db8 |
| oracle/crm/src/main/java/corpuscrm/crm/contacts/registercontact/RegisterContact.java | b86e224225618af2b32f8ab809e335bc0b8e3a7e787f0819f495ae2d774a7af1 |
| oracle/crm/src/main/java/corpuscrm/crm/contacts/registercontact/RegisterContactHandler.java | ade24824e6b7e39656c05d14bc2339c4380996c4f85153e6d2c1db14ea75ba26 |
| oracle/crm/src/main/java/corpuscrm/crm/contacts/updatecontact/UpdateContact.java | 3767c0c11289b29a3a5cf56bf0db1091d1b94661bed5fca8227e429b49adab79 |
| oracle/crm/src/main/java/corpuscrm/crm/contacts/updatecontact/UpdateContactHandler.java | 576c4460806a4150c767530e0ffbea9c0fde6739cc7f19bc127a60492e278eca |
| oracle/crm/src/main/java/corpuscrm/crm/deals/movedeal/MoveDeal.java | 2bd5d7148fd37cf2327da930846fbf57ff822a25cf29bf2650fafce66d07c1b0 |
| oracle/crm/src/main/java/corpuscrm/crm/deals/movedeal/MoveDealHandler.java | e0b0ad022d7b3f6a82b3016f200e540d900a90e2f1e9c140dc2b82b66b6751de |
| oracle/crm/src/main/java/corpuscrm/crm/deals/registerdeal/RegisterDeal.java | ef40b3e044c316d24eeb44500bf5b0611a5e7a198bd0825cd2fb91e0184f3169 |
| oracle/crm/src/main/java/corpuscrm/crm/deals/registerdeal/RegisterDealHandler.java | 81549e60c0873e710dd5501dcb24531133bc4ea7b3ca69ceb08e04c200e87a38 |
| oracle/crm/src/main/java/corpuscrm/crm/deals/updatedeal/UpdateDeal.java | b86988bbd44a93334ce57e5c11e55d510b0d12b129f089e6dd9ce38f34af7b74 |
| oracle/crm/src/main/java/corpuscrm/crm/deals/updatedeal/UpdateDealHandler.java | 3c147249c050dc2878644aeac7659f25ac034f6eefc2c17c431814ff2da59acc |
| oracle/crm/src/main/java/corpuscrm/crm/events/ActivityRegistered.java | f2e5cecd445135917d9761ed1e261366484e5d67d975936aef39c71cf0731ed7 |
| oracle/crm/src/main/java/corpuscrm/crm/events/ActivityUpdated.java | 01dc4e5e4785c09660cf26a3d5a35693027286b94d4ead6419db55adaf7bef00 |
| oracle/crm/src/main/java/corpuscrm/crm/events/CompanyRegistered.java | 404a7e88ec157e9a4e941ad9eab38df69dfc160272bf1f83a57fb0f9d3468081 |
| oracle/crm/src/main/java/corpuscrm/crm/events/CompanyUpdated.java | f70e6ac4fd5f518e1a28f46a0be3d85c1979f0f92c10c51508466b32de505778 |
| oracle/crm/src/main/java/corpuscrm/crm/events/ContactRegistered.java | 5ae9c4fe1246ce9397f16bf52c556dbef21057fae8874d2e91adc798899665fb |
| oracle/crm/src/main/java/corpuscrm/crm/events/ContactUpdated.java | 187e6c9dbe46fb288486c63790eceb63d268eb8a416e25bf163f317771e36b1f |
| oracle/crm/src/main/java/corpuscrm/crm/events/DealMoved.java | 8c649a5437777a50b0b78f4844f85c9b6212d1b9c004555cb3d8861a52420d23 |
| oracle/crm/src/main/java/corpuscrm/crm/events/DealRegistered.java | 98fd71e6c080a7aab8b619145f3d7a372fa27f1f62a1d9e690c8645e41d65b2c |
| oracle/crm/src/main/java/corpuscrm/crm/events/DealUpdated.java | 9cce348103896dd17281bbad4b01a854a8918d7bef3c4c3efd06957160395be7 |
| oracle/crm/src/main/java/corpuscrm/crm/events/EventTags.java | f60be5a2e5eee16cfa614d5ca0ba978af75032bf44acfb8605451958564e795f |
| oracle/crm/src/main/java/corpuscrm/crm/events/LeadRegistered.java | 85ea401ea176015b21ee8d2a2af14118dc04ab29d343f217d89670bb86edc3a4 |
| oracle/crm/src/main/java/corpuscrm/crm/events/LeadUpdated.java | 97d9e63319ae4b2a7fcfe927ba5fa9092e97e7c6e415b5747a3f9df40933cdb3 |
| oracle/crm/src/main/java/corpuscrm/crm/leads/registerlead/RegisterLead.java | 94dab97612ce1fddf473bda943564ee710920c2df0480c2f44e2806acc205f3e |
| oracle/crm/src/main/java/corpuscrm/crm/leads/registerlead/RegisterLeadHandler.java | 91f95d6805c4e69da736de1d32d2e61cd0dce55a5f17fd7f087daecdef4d3aca |
| oracle/crm/src/main/java/corpuscrm/crm/leads/updatelead/UpdateLead.java | 28c442a9f0ef6165af8f704c8e8900f5ed755f680a9d72670f8f2d61661a57d3 |
| oracle/crm/src/main/java/corpuscrm/crm/leads/updatelead/UpdateLeadHandler.java | 2ec37eb7da022fe798a05f86632d105259a42aabeb3809d10a0a86e08529772c |
| oracle/customer-portal/src/main/java/corpuscustomerportal/portal/events/EventTags.java | 358428d5097d212a7a423aee115c2da91f8d036eb3b51adc9806788a01a8485b |
| oracle/customer-portal/src/main/java/corpuscustomerportal/portal/events/PortalInvoiceRegistered.java | f8c76083de235d36278f1ca34ea33beee7b0f54aa6dc849e50b6e61092c38289 |
| oracle/customer-portal/src/main/java/corpuscustomerportal/portal/events/PortalOrderRegistered.java | 1a141eb27344858aa997ff99c515541985e51e31ba3d9bcf46739bdfedab149e |
| oracle/customer-portal/src/main/java/corpuscustomerportal/portal/invoices/registerportalinvoice/RegisterPortalInvoice.java | e59e866765c409f71431be46e17957b0e095fd0f2d7691aeb4b4d8b59f0eacc2 |
| oracle/customer-portal/src/main/java/corpuscustomerportal/portal/invoices/registerportalinvoice/RegisterPortalInvoiceHandler.java | f8575289ca964a9060b0a4889188c0e6fb15a94797a427522d210a62c1c3ec2f |
| oracle/customer-portal/src/main/java/corpuscustomerportal/portal/orders/registerportalorder/RegisterPortalOrder.java | 924261123c63f43fe3c92f3d10b357f8a1d1e042637a90edc1e5e664f2d525c6 |
| oracle/customer-portal/src/main/java/corpuscustomerportal/portal/orders/registerportalorder/RegisterPortalOrderHandler.java | fbbafa9082c8a744d9389eabafda55967a11c18449c2f8f5ee352222f136ee80 |
| oracle/hr/src/main/java/corpushr/hr/directory/registeremployee/RegisterEmployee.java | a0d2e2bdfebeb9894b6efc10acb6dfe2152d740b378c7be2914b95088addebf2 |
| oracle/hr/src/main/java/corpushr/hr/directory/registeremployee/RegisterEmployeeHandler.java | 7c9f1c56ad2a63e9eadc3ccaae244d8ccd5f2d096c6b22374cea6eeb1350a2ef |
| oracle/hr/src/main/java/corpushr/hr/events/BalanceRegistered.java | 152bfeab589ab745f425cdb5e2fc5765f2acd0f712368cf03347d6bea4b2536a |
| oracle/hr/src/main/java/corpushr/hr/events/EmployeeRegistered.java | ddba52874bccd39c5c88dad30c178c8936c7c53eea525d6363b6a7735ba8fd8f |
| oracle/hr/src/main/java/corpushr/hr/events/EventTags.java | 6702df6bb4f7c0628727c7a47feaffaacd3497f6cfa4838d694517ce8feee5b2 |
| oracle/hr/src/main/java/corpushr/hr/events/OnboardingAdvanced.java | ae9c6e21d25a9a74fc102916ee6926242ba5faa87b18bd07722c14c8352e2d7c |
| oracle/hr/src/main/java/corpushr/hr/events/OnboardingRegistered.java | cef22bee9f2c0de60a9287bf00051f08424fdb2fb78259d19dd43a7957ea2155 |
| oracle/hr/src/main/java/corpushr/hr/events/TimeOffDecided.java | 05ee61e90920f80c588a0ca3634c9a5cc417297ce639aaa492c126650732dcaf |
| oracle/hr/src/main/java/corpushr/hr/events/TimeOffRequestRegistered.java | fe6026fa02f8e698afe90b04b4aa9a22c3b7c106cc78d7fcca38a16b779ae326 |
| oracle/hr/src/main/java/corpushr/hr/onboarding/advanceonboarding/AdvanceOnboarding.java | 3246d7473cf04e1a3c1d87117c26bb06e0871fff438422ef5310a4fa02010abe |
| oracle/hr/src/main/java/corpushr/hr/onboarding/advanceonboarding/AdvanceOnboardingHandler.java | 87eb910e86bbcf1d0e5ebea396473b709f673a9d67b5bda375b70a30142bed29 |
| oracle/hr/src/main/java/corpushr/hr/onboarding/startonboarding/StartOnboarding.java | 121a0c3dfffefd0684f91a5591e257a827a65453647114c114e7b4f7981ba84f |
| oracle/hr/src/main/java/corpushr/hr/onboarding/startonboarding/StartOnboardingHandler.java | 46f17851466c5f21c2287d352b8df0c5407362cdfba264ad4f82d1cf88c26860 |
| oracle/hr/src/main/java/corpushr/hr/timeoff/balances/openbalance/OpenBalance.java | 068835578c87324898a6ceda5a16aedb485abc3ad34a1bf972c589554082840c |
| oracle/hr/src/main/java/corpushr/hr/timeoff/balances/openbalance/OpenBalanceHandler.java | dbf0b393288c00511801934406122407598e6476d6f47387cf996bc94a74e395 |
| oracle/hr/src/main/java/corpushr/hr/timeoff/decidetimeoff/DecideTimeOff.java | 66ea1dfac488ae852d9a2f8b63891555a18d065344df8bab545c053900387dff |
| oracle/hr/src/main/java/corpushr/hr/timeoff/decidetimeoff/DecideTimeOffHandler.java | 558171ee25cf9f2df594fa41afdf0602642c2f4337ed484d48eb44cbc3d5aded |
| oracle/hr/src/main/java/corpushr/hr/timeoff/requesttimeoff/RequestTimeOff.java | 208052958d0fbae240b3f3d9f0ffad2f3bad6693478d7e9cd37ec0366dee06f9 |
| oracle/hr/src/main/java/corpushr/hr/timeoff/requesttimeoff/RequestTimeOffHandler.java | ba465839d54bc43df702da3741afddc6e67a2d8f654427ab9e7f9aa40ce7022a |
| oracle/inventory/src/main/java/corpusinventory/inventory/events/EventTags.java | cf3a0dfdbc5445d92104fbefd0f0d1151c54f79f0809ddb5e94011400eba8545 |
| oracle/inventory/src/main/java/corpusinventory/inventory/events/ItemRegistered.java | dcf09af998d92e804fe87c4976fe30006b005a2cb3204969d4c716627c5acafa |
| oracle/inventory/src/main/java/corpusinventory/inventory/events/LocationRegistered.java | 7d9408a56dfbfd2f22f006d38f4d832fef4259aa3a6012741483dc40b1862481 |
| oracle/inventory/src/main/java/corpusinventory/inventory/events/MovementRegistered.java | b597ad05405e2d43fb7a56422edb1f47b0e31c2c8982beb3be9b22e3564d185f |
| oracle/inventory/src/main/java/corpusinventory/inventory/events/StockAdjusted.java | ed28375f33996a25bd891881ae08019078838288dbf0ecef0ef4d930864ac3ec |
| oracle/inventory/src/main/java/corpusinventory/inventory/events/SupplierRegistered.java | 7a50a755a8a197ba280e171856ea9efcd155584ba4d0d44ede40dc8cca58cde9 |
| oracle/inventory/src/main/java/corpusinventory/inventory/items/adjuststock/AdjustStock.java | 7697a5c887603201dfaabb1ff56b32564ccf348ee94776ff3e46e474db08a77a |
| oracle/inventory/src/main/java/corpusinventory/inventory/items/adjuststock/AdjustStockHandler.java | cf16e214c0825cf88678772f63bdb204964775b82f4e08cde0848bad8c53fc8a |
| oracle/inventory/src/main/java/corpusinventory/inventory/items/registeritem/RegisterItem.java | 67f2ce10eff1b67bb09601096545b6bbd2aeeaa012f5d569572b4159b75a6412 |
| oracle/inventory/src/main/java/corpusinventory/inventory/items/registeritem/RegisterItemHandler.java | 5862546e79769ba93c70042aa3141bc262fa24d5dc730887c2b815488a50709d |
| oracle/inventory/src/main/java/corpusinventory/inventory/locations/registerlocation/RegisterLocation.java | d69cfc5b5c6a8cfba6e6f5f11e80b0c88abd8c29607ddbecf4ed13f44ab0eda0 |
| oracle/inventory/src/main/java/corpusinventory/inventory/locations/registerlocation/RegisterLocationHandler.java | 0e506e075a6355b6723a58859ab8b734f13201241750eaa1e4dce6387685122b |
| oracle/inventory/src/main/java/corpusinventory/inventory/movements/registermovement/RegisterMovement.java | 51a1221e45b9837e575baf8f29e75c36a3938d1727cb6ac98e708a51e01dfa61 |
| oracle/inventory/src/main/java/corpusinventory/inventory/movements/registermovement/RegisterMovementHandler.java | a1ebf20cf1c20a920872f667a69f8783e24f608d5f14bedde457b0ee43be9776 |
| oracle/inventory/src/main/java/corpusinventory/inventory/suppliers/registersupplier/RegisterSupplier.java | ede6e145ccd9b3a1d2c7e1b32374445d09ebd2010dfa2e61baa59f92896a52ed |
| oracle/inventory/src/main/java/corpusinventory/inventory/suppliers/registersupplier/RegisterSupplierHandler.java | 8e6844dd85b5b889a1fce5e066adda945cdc7db69d6161b5783b0cd963317986 |
| oracle/invoicing/src/main/java/corpusinvoicing/invoicing/events/EventTags.java | 815fcb17c0ad47842b4128f016030d05041667720970ac9bc4c3626f6a55d06b |
| oracle/invoicing/src/main/java/corpusinvoicing/invoicing/events/InvoiceRegistered.java | e204fe0343212e32bdc5abe602d1586b478814b4efb131c7c872637843c19e79 |
| oracle/invoicing/src/main/java/corpusinvoicing/invoicing/events/PaymentRegistered.java | 2b118f9bdfa41ed97ab3eb4f5312a8fe548239183703c5cc6861e4fb306d64dc |
| oracle/invoicing/src/main/java/corpusinvoicing/invoicing/invoices/registerinvoice/RegisterInvoice.java | 78c28990b37469907df59c76c65cbc2b92374c8fa84835f4bfb116a124bc2f35 |
| oracle/invoicing/src/main/java/corpusinvoicing/invoicing/invoices/registerinvoice/RegisterInvoiceHandler.java | 4ecdbf52d9746e4879f4c8b9cf679c00eed9acfbcbdf61133584e0a85ec831a4 |
| oracle/invoicing/src/main/java/corpusinvoicing/invoicing/payments/registerpayment/RegisterPayment.java | 5a7c483c6f9da1d6fa0d135bd89b257fcbba151edbe5f4f1ae18399f6a6ad861 |
| oracle/invoicing/src/main/java/corpusinvoicing/invoicing/payments/registerpayment/RegisterPaymentHandler.java | 707993af568e2a340a3b47816a75eb028d66054df97ea84ba10f09cf0e3ef926 |
| oracle/orders/src/main/java/corpusorders/orders/events/EventTags.java | 5fc0f251c5c9c86265738f74407202742ed47a16dbda0fc35c487557cf065d0e |
| oracle/orders/src/main/java/corpusorders/orders/events/OrderMoved.java | c6f1c46198c4a225d79e73d9d1b4eafe51c5f385f4a591215797f7628bbd0c03 |
| oracle/orders/src/main/java/corpusorders/orders/events/OrderPlaced.java | 5a921883278aea9378c0c12dd52fb6b4a65343d1d027967cfeffd7f72d85b629 |
| oracle/orders/src/main/java/corpusorders/orders/fulfilment/moveorder/MoveOrder.java | d4e8fa052ac695551a73fee3f8a393b2de8a44fa01a0a0192e2c57dd0ab9148a |
| oracle/orders/src/main/java/corpusorders/orders/fulfilment/moveorder/MoveOrderHandler.java | 65006c5be3b206855312da60a8182110bd0306ba0f57fbd532f7aa2537afb097 |
| oracle/orders/src/main/java/corpusorders/orders/orders/placeorder/PlaceOrder.java | 8d7c2f7f297355da2f2f29b47cada4ab10b773b708ea5a167eb6f74c7253bba7 |
| oracle/orders/src/main/java/corpusorders/orders/orders/placeorder/PlaceOrderHandler.java | ee2ea3ff925bd2355b777ecad7b1e3e5e7debfd201f1b47cf6b3cfd10287e3f0 |
| oracle/projects/src/main/java/corpusprojects/projects/board/movetask/MoveTask.java | d48dc354e09b506f752dc385241e68b6e5bfc01c900507aa9565055905180837 |
| oracle/projects/src/main/java/corpusprojects/projects/board/movetask/MoveTaskHandler.java | f0d9442b454977ade0e7d5d008408bd040308e22e9575f1a18c59a89e6e08135 |
| oracle/projects/src/main/java/corpusprojects/projects/events/EventTags.java | d8613bfc295f70cae2ad7c1c24d316f864dd2171cc4ee6aad67d57d27ed3f84b |
| oracle/projects/src/main/java/corpusprojects/projects/events/ProjectRegistered.java | 60e8eb4b825aa814f1fdf3364977bc6c5c154e0c37012bec36a84d891a600e1e |
| oracle/projects/src/main/java/corpusprojects/projects/events/TaskMoved.java | 07a218aa200d0d0cc4240d4f98e65092b8032a016b0f879a40448715c6220cb9 |
| oracle/projects/src/main/java/corpusprojects/projects/events/TaskRegistered.java | 9d6ac03e3c88cd8448e16b779c85eecf18fbeb9fed71d522fb5462311c50e785 |
| oracle/projects/src/main/java/corpusprojects/projects/mytasks/registertask/RegisterTask.java | 0810d08e3950fe612a911af0f55329d23c0704b751f8f2c19d3a91c9de49eb5c |
| oracle/projects/src/main/java/corpusprojects/projects/mytasks/registertask/RegisterTaskHandler.java | d7e380c38c76b176a1f2a0e24d73a7bd468cda0e24a3621dd8164622f114c617 |
| oracle/projects/src/main/java/corpusprojects/projects/projects/registerproject/RegisterProject.java | 2beba683b37dbca93f1e69ba7f05c22d4aacf55b2422d273ce80886311192779 |
| oracle/projects/src/main/java/corpusprojects/projects/projects/registerproject/RegisterProjectHandler.java | 4ce1a4cd5a5e3c2d4f85f211f55b73d44c69afeda71886092990f0be16ac5947 |
| oracle/sample-custom-packages/src/main/java/corpussamplecustompackages/workspaces/events/CommentAdded.java | a64fa2ea58b06eea6f1bc6f548f284b6a52e1784fe4e8d481503cc5582231bf5 |
| oracle/sample-custom-packages/src/main/java/corpussamplecustompackages/workspaces/events/EventTags.java | 1bf9f885af084a38cc30a14c31bc58d8e5502a0cbae639347787e53b9ac5de16 |
| oracle/sample-custom-packages/src/main/java/corpussamplecustompackages/workspaces/events/WorkItemClosed.java | 732040f7eb13d3e1fc576a79b04ef0b8b7f80b0cfe0ccaa7ffcd16632df8f0ce |
| oracle/sample-custom-packages/src/main/java/corpussamplecustompackages/workspaces/events/WorkItemCreated.java | 7db74863258443d7154707032be55d8b700a7bd94c7602bef18c02c15f55ad95 |
| oracle/sample-custom-packages/src/main/java/corpussamplecustompackages/workspaces/events/WorkItemRenamed.java | 1580fbb148cabb8288152c7f9b02ccf99b800091c9f9a93d6d7ba1f626ed4989 |
| oracle/sample-custom-packages/src/main/java/corpussamplecustompackages/workspaces/tracking/addcomment/AddComment.java | 738b01231072aa97c83129e1656f15149a7db1c330fb7d29c8063d0354098555 |
| oracle/sample-custom-packages/src/main/java/corpussamplecustompackages/workspaces/tracking/addcomment/AddCommentHandler.java | f724f6bada732e13d8cb4ed9f4e49c1cf3a11b99d19e183ce57adf3355f77fc0 |
| oracle/sample-custom-packages/src/main/java/corpussamplecustompackages/workspaces/tracking/closeworkitem/CloseWorkItem.java | 5b8fba8d7b0db9f07a0ea6269e7edc67b19f2750ff202d265d29fd5e2384ac6b |
| oracle/sample-custom-packages/src/main/java/corpussamplecustompackages/workspaces/tracking/closeworkitem/CloseWorkItemHandler.java | d22d33bed6e9d7db179a1a0ba41c9612f940a05e36aee397bda2ac66b544b6ed |
| oracle/sample-custom-packages/src/main/java/corpussamplecustompackages/workspaces/tracking/createworkitem/CreateWorkItem.java | f29546211748d9ca8f88733ff693b05d1664712110a30d160df5bfa7ad8ae117 |
| oracle/sample-custom-packages/src/main/java/corpussamplecustompackages/workspaces/tracking/createworkitem/CreateWorkItemHandler.java | 670c6f27747770cf36b957e3a8077d659012020a832e45e53eae62e778d63f4e |
| oracle/sample-custom-packages/src/main/java/corpussamplecustompackages/workspaces/tracking/renameworkitem/RenameWorkItem.java | 905f5c6ed58900641c7dd845b484bb7faaa708f5c7a8bc980346f41af5e83215 |
| oracle/sample-custom-packages/src/main/java/corpussamplecustompackages/workspaces/tracking/renameworkitem/RenameWorkItemHandler.java | c99f87284aa80f20b92a9fdf9653fcdf9aba490fe8be4d753a078847fb962f59 |
| oracle/sample-navigation/src/main/java/corpussamplenavigation/sales/events/EventTags.java | ba79187bec8520ebd0d2531add71e711ed8d9fd4ccddfdeba433e21bef1d8f1b |
| oracle/sample-navigation/src/main/java/corpussamplenavigation/sales/events/OrderPlaced.java | 017c0b84a42cf8b27f94806e24228008a3ac1e73c55391c9e7ac3e09da02b7f1 |
| oracle/sample-navigation/src/main/java/corpussamplenavigation/sales/orders/placeorder/PlaceOrder.java | 7a1de6c3759d42b9ce40107d768d2fb362e4537d04b28012dc4c03601dda1047 |
| oracle/sample-navigation/src/main/java/corpussamplenavigation/sales/orders/placeorder/PlaceOrderHandler.java | 4e912a0c9752a5767dab8ac396a701d78222d68908223d1730d7ae4e89380288 |
| oracle/sample-templates/src/main/java/corpussampletemplates/customers/customers/registercustomer/RegisterCustomer.java | 998e70e420d2efaf3c2a17d9bd0a629c624b509a01db9156a2a16ba316ae4285 |
| oracle/sample-templates/src/main/java/corpussampletemplates/customers/customers/registercustomer/RegisterCustomerHandler.java | 114315d71902230f7cadb09d009b7885f54f7c76fe53fc2103d340b921b56b1d |
| oracle/sample-templates/src/main/java/corpussampletemplates/customers/events/CustomerRegistered.java | 3e81c1108066d349100d204401da83bc360d9028f39db590b357b17b92ce2378 |
| oracle/sample-templates/src/main/java/corpussampletemplates/customers/events/EventTags.java | cea1183bb26c99ef430e65e054ceacdf5c054b03efb4d8bc673e469f822b8793 |
| oracle/ticketing/src/main/java/corpusticketing/support/customers/addcustomer/AddCustomer.java | 9bd84a8659a52006df23c8555c790373e3284d90832a19ecc9b4b62a32a1f41d |
| oracle/ticketing/src/main/java/corpusticketing/support/customers/addcustomer/AddCustomerHandler.java | 7d5e45a2a112a2614b07efa60e2540f87713c97ac24f66a7d41ccd4c9416e948 |
| oracle/ticketing/src/main/java/corpusticketing/support/events/ArticlePublished.java | b0a99b326b1ddd06406e7fe11d5e342ed7174ef84961c73d8b5a5ec05face5d0 |
| oracle/ticketing/src/main/java/corpusticketing/support/events/CustomerAdded.java | f9dfe593d525d63f820c844993e2367d2720c28f52f1e649216079d41b88443e |
| oracle/ticketing/src/main/java/corpusticketing/support/events/EventTags.java | e9cdd991259ced18577e56e9add939b69c2fe806d127ccb32676ca3ce4ff5ea5 |
| oracle/ticketing/src/main/java/corpusticketing/support/events/TicketAssigned.java | e78c48de26253e4a0db9a6c767bd42c0054ff0bf3489a5df40dc2a96346dbd6c |
| oracle/ticketing/src/main/java/corpusticketing/support/events/TicketEscalated.java | e70e4f518dcbc5899382ace1285a33cde128da296a5f43c29d1be634de9ac478 |
| oracle/ticketing/src/main/java/corpusticketing/support/events/TicketOpened.java | b8a4142b069d55763781072cd8195d6fac5988644c1e29f5c85d26238588bbb6 |
| oracle/ticketing/src/main/java/corpusticketing/support/events/TicketStatusChanged.java | 7ce8e1b7fd7986ebc105a8a136367e512148da6a45f69ec8c181e18f116a2993 |
| oracle/ticketing/src/main/java/corpusticketing/support/events/TicketsMerged.java | 989044b3638d8a8eb6de7236f99b56945f6fe1d0687b7cf086ba75202d4ef126 |
| oracle/ticketing/src/main/java/corpusticketing/support/knowledge/publisharticle/PublishArticle.java | 48402f5acbd39309d14cc5ba45e2bf1f9f67b128d4f5dfd95fc36d23ae66e283 |
| oracle/ticketing/src/main/java/corpusticketing/support/knowledge/publisharticle/PublishArticleHandler.java | cbec3dac5985a4f14a2c2723f09c8d3421a7bd9d287c1c787d64c28c8c9b02aa |
| oracle/ticketing/src/main/java/corpusticketing/support/queue/assignticket/AssignTicket.java | 900fb13bedaed62482df47dd6c54494ec966a4ebfebd7c5264c3f4509cdc6a52 |
| oracle/ticketing/src/main/java/corpusticketing/support/queue/assignticket/AssignTicketHandler.java | 2cef3126e0d621ae7d88a9dfdb22ddf6f459e7638b20b5f0409ac1937b1df888 |
| oracle/ticketing/src/main/java/corpusticketing/support/queue/changeticketstatus/ChangeTicketStatus.java | 297483c0aa30bc325e0c1e45964a19b6e8b4875bdaeb60d0b77a329c797eed43 |
| oracle/ticketing/src/main/java/corpusticketing/support/queue/changeticketstatus/ChangeTicketStatusHandler.java | dc6ae670015111401cecd7eadbb4ab747a8a29106bef773bb00a419920ffbbad |
| oracle/ticketing/src/main/java/corpusticketing/support/queue/escalateticket/EscalateTicket.java | cecc4358355086b169ea890f8bb3bb10b28d2e270cb62d4a74d8897a315a4b21 |
| oracle/ticketing/src/main/java/corpusticketing/support/queue/escalateticket/EscalateTicketHandler.java | d8406eeb162f7097435d341dc6ac48f99b94747f237520ca1a52f9d30d9689d0 |
| oracle/ticketing/src/main/java/corpusticketing/support/queue/mergetickets/MergeTickets.java | dab345f5579e6180d86def284c7648c8f44783f3e26bbb250226a84601c8fead |
| oracle/ticketing/src/main/java/corpusticketing/support/queue/mergetickets/MergeTicketsHandler.java | 31eac3ee3d717aa6a55cfcf9cc41c5161a90ece7ebea13c19ba9b55d5a993080 |
| oracle/ticketing/src/main/java/corpusticketing/support/queue/openticket/OpenTicket.java | 0dfce3759fc7bb636e2a7b76a6393947a42d6bab2b1a68c8a3f8444a1dc5e1d0 |
| oracle/ticketing/src/main/java/corpusticketing/support/queue/openticket/OpenTicketHandler.java | c6f5d2ee31044f34f0bacb0351db5d86a944f7123f03b8d3b29e4abe74483a80 |
| oracle/vendors/src/main/java/corpusvendors/vendors/events/EventTags.java | 1c63cf40072a3b423a22cf20f9301c5e18d39e24689f9e7973ddb391e0ff2bfd |
| oracle/vendors/src/main/java/corpusvendors/vendors/events/VendorMoved.java | 16fb6487af9cb2db25b87e112682e0f55b080dd3252c6f88a2c8970394c0647f |
| oracle/vendors/src/main/java/corpusvendors/vendors/events/VendorRegistered.java | c9efa1d1982ee2546c3b7658301f2ebac56947f5bd547088c3f084fc5b5df07e |
| oracle/vendors/src/main/java/corpusvendors/vendors/onboarding/movevendor/MoveVendor.java | 6a4a81fbd60b9a32b0405ce8ef9a137d9af2d32b0049e9fa4123fa595cd67493 |
| oracle/vendors/src/main/java/corpusvendors/vendors/onboarding/movevendor/MoveVendorHandler.java | d9696b0fa2f017969f7015d6f1865351d508c65a2885253a8c3abb98ed505ad1 |
| oracle/vendors/src/main/java/corpusvendors/vendors/vendors/registervendor/RegisterVendor.java | 8021d4b4d829ccbd089913c53e9ffb014c52e8bb1bf4d7ec38c0aa30728e0d85 |
| oracle/vendors/src/main/java/corpusvendors/vendors/vendors/registervendor/RegisterVendorHandler.java | 803d549059c91b0f3499ffa61eb233d029c5ff4e8935f0d29d561640f75f124b |
| oracle/view-composite-keys/src/main/java/corpusviewcompositekeys/metrics/events/EventTags.java | d1de6f06eb7c75cfcc75e25cbb99b26e53d357ec4a97d352c0f6529d31087bc8 |
| oracle/view-composite-keys/src/main/java/corpusviewcompositekeys/metrics/events/MetricCounted.java | e67688e967b5183c8bca86c33ed20b11062b2ef162a493fc0b688bee0c3117c4 |
| oracle/view-composite-keys/src/main/java/corpusviewcompositekeys/metrics/events/MetricRemoved.java | 2ddc0454061a3add7f589e886371edc2d6bce69542c05abb304219c498b43a32 |
| oracle/view-reducers/src/main/java/corpusviewreducers/reduction/counters/record_/RecordCount.java | 7020001066bccdae8483d59ca8ceea5fae0ea7b18f718f7c8fddf93bc13e63a7 |
| oracle/view-reducers/src/main/java/corpusviewreducers/reduction/counters/record_/RecordCountHandler.java | 370291d8f237ed0d61f15897a8df54839d4255eac2bddd48c8faf2dd0e3f1c51 |
| oracle/view-reducers/src/main/java/corpusviewreducers/reduction/events/CountRecorded.java | f674460d32b97c911e0718a63ea5ccc6da2796fa745d8cdec9a23ef47ad2208c |
| oracle/view-reducers/src/main/java/corpusviewreducers/reduction/events/EventTags.java | 0516da342efc9a7fe2557c92203d7b8e0f390d9bd558dd5480beb86e6e3239cb |
| oracle/view-scopes/src/main/java/corpusviewscopes/scopes/boards/writeboards/AddCard.java | 349cfe147a97cfd28a885e99b2c79e9eefbc7816ad86a9a5ebd42b5868a985c2 |
| oracle/view-scopes/src/main/java/corpusviewscopes/scopes/boards/writeboards/AddCardHandler.java | e31fcd02e63ca08345ff3fc7d3a06e76d48541a8bca4009c8a8cedcaf5c4a11d |
| oracle/view-scopes/src/main/java/corpusviewscopes/scopes/boards/writeboards/ClearDetails.java | 47155aed9ff95af287b19bd23ebedf430d146fbe87b119e2bb795b9f537ff2da |
| oracle/view-scopes/src/main/java/corpusviewscopes/scopes/boards/writeboards/ClearDetailsHandler.java | 584e000ad782aea3a6d7740e687fc83933de6b4f46e734cb7cd0dc1895c0787d |
| oracle/view-scopes/src/main/java/corpusviewscopes/scopes/boards/writeboards/CreateBoard.java | cf187aa0d343eaeb27968a753e4b920e247d96a2e940a41558708ce59c3e40cd |
| oracle/view-scopes/src/main/java/corpusviewscopes/scopes/boards/writeboards/CreateBoardHandler.java | aa4942b478ace149eb8387a5d1d8f1c03d5dd801518cb1667cd2d6f4801adfa2 |
| oracle/view-scopes/src/main/java/corpusviewscopes/scopes/boards/writeboards/PurgeCard.java | f2174b2ae3f1311593b30f92cb4810c7296ffae123ce58610f71d7a306f56c33 |
| oracle/view-scopes/src/main/java/corpusviewscopes/scopes/boards/writeboards/PurgeCardHandler.java | d05df8199d17bb31bcf7e53ded98bf9971cf2f623750684ee75d6c9f6179e9c8 |
| oracle/view-scopes/src/main/java/corpusviewscopes/scopes/boards/writeboards/RemoveBoard.java | c6e08e7eb8fc44d3449e468c1bdfc3b1fe0c6efa21155474279c7d84d51df0db |
| oracle/view-scopes/src/main/java/corpusviewscopes/scopes/boards/writeboards/RemoveBoardHandler.java | 647a0a96c51f118e693d0e91751a5d92de75d91dd8c923ea707bd85fa1323f6a |
| oracle/view-scopes/src/main/java/corpusviewscopes/scopes/boards/writeboards/RemoveCard.java | 50165294c6a20fdaee1829132e05331ea1434d5f60f1dda5e93e4b9abccc47a7 |
| oracle/view-scopes/src/main/java/corpusviewscopes/scopes/boards/writeboards/RemoveCardHandler.java | 50b22aac199daa910b0fa0cd62ea26db2fdb90345b14e0882202b0184bf6d5a9 |
| oracle/view-scopes/src/main/java/corpusviewscopes/scopes/boards/writeboards/RenameCard.java | 174b8b29bf327970612da6a9a848bb630ab96213990da3306cecca04ebab159b |
| oracle/view-scopes/src/main/java/corpusviewscopes/scopes/boards/writeboards/RenameCardHandler.java | e1deeeadfcc33548efecbb2e350eeeb693b323d747fb9d764e1c9612bd8c1ff9 |
| oracle/view-scopes/src/main/java/corpusviewscopes/scopes/events/BoardCreated.java | 7f0ccc39ed86fc6ad33207f46cdb7167d769057d7f5c80f5ca15c7ea500972f1 |
| oracle/view-scopes/src/main/java/corpusviewscopes/scopes/events/BoardRemoved.java | b7b1540bc4de2c3d3c2e5ba8ed026ddf5ceab3ed1278f4b1c76db562fc0e1d9f |
| oracle/view-scopes/src/main/java/corpusviewscopes/scopes/events/CardAdded.java | 2c737d3f742cf1d80e93172b537b907b0cf6f8adc872725e4d3ac900b3b1d362 |
| oracle/view-scopes/src/main/java/corpusviewscopes/scopes/events/CardPurged.java | a1552ca93da5d3360654e4a195a77e3789042a0b04edcf90300edb6becfeea91 |
| oracle/view-scopes/src/main/java/corpusviewscopes/scopes/events/CardRemoved.java | 82c14098a549f46bdd68dfe747b000b49f770ed28d1229ca7efe17d201fea03f |
| oracle/view-scopes/src/main/java/corpusviewscopes/scopes/events/CardRenamed.java | 721f611a3ff4c6d8c1b3c539c4c9c16388e4c1713f91b94e1b1de77d06177dc9 |
| oracle/view-scopes/src/main/java/corpusviewscopes/scopes/events/DetailsRemoved.java | 39cd07395d4bab15cb9e573d73533a6401a33d3dc893c5d63bbc95c932f46d8f |
| oracle/view-scopes/src/main/java/corpusviewscopes/scopes/events/EventTags.java | d44a0dcdb1d5ca4786044c2f7e0d78ecea2fc63fd96e4bcf51c6d60ebc1e2e25 |
| oracle/view-views/src/main/java/corpusviewviews/views/counters/decrease/Decrease.java | 5acecf6313999b3c4c2a615c299811a4e6609cee66e1974d3846b7d2fdce71fc |
| oracle/view-views/src/main/java/corpusviewviews/views/counters/decrease/DecreaseHandler.java | 10bb8b04c893b1021c8402a9f0d267698b2ad382ac91b0ed7c3ba9f7c8bc282e |
| oracle/view-views/src/main/java/corpusviewviews/views/counters/increase/Increase.java | 2efd14e9d46aa0bb2590a52171251f3880486ebc0441d4561c9af9d29c38be9c |
| oracle/view-views/src/main/java/corpusviewviews/views/counters/increase/IncreaseHandler.java | 31eefefb8cef10d91c3602807bbb023e90bc3be41adca6025f2b8b2952805b2b |
| oracle/view-views/src/main/java/corpusviewviews/views/events/Decreased.java | 233a3d38c0a909df1467145d9322e15b28a12c035ef321d859d72df64ceaba23 |
| oracle/view-views/src/main/java/corpusviewviews/views/events/EventTags.java | ccd8139090af50380ae5518934888834e4d8c423a6bd8fa362860dcf8212233c |
| oracle/view-views/src/main/java/corpusviewviews/views/events/Increased.java | 22926137743d93d35317410206d6d97607e9ff294602927d59cbc75872371d29 |
| oracle/views-combined/src/main/java/corpusviewscombined/metrics/events/EventTags.java | ec2fd833630dea3960d0c77d24dc9ab7bbefb0d6ef4739d3d519064322f7d85d |
| oracle/views-combined/src/main/java/corpusviewscombined/metrics/events/MetricCounted.java | 6688c72c9ebfcffab57305c6307107ada3952fc19ef057efc13e5d0798e47673 |
| oracle/views-combined/src/main/java/corpusviewscombined/metrics/events/MetricRemoved.java | 322aeef98cfd9c7866e47ca04cf28bb8ceed215e497dd8984cfad8a72821be0e |
| oracle/views-combined/src/main/java/corpusviewscombined/reduction/counters/record_/RecordCount.java | 681409b5fc70d8f83094d4bea0c2bb1046c612c5ded407fbb1e83ff2548c306c |
| oracle/views-combined/src/main/java/corpusviewscombined/reduction/counters/record_/RecordCountHandler.java | a34d91e971b77787edf750290417643661dc14f1f9d8f491214fffd7e33cd4e3 |
| oracle/views-combined/src/main/java/corpusviewscombined/reduction/events/CountRecorded.java | 26bca128e7c1f4062f90c6f3b5574d744c81c912c6aa2d109f02b2dd160e7394 |
| oracle/views-combined/src/main/java/corpusviewscombined/reduction/events/EventTags.java | fb005effd4a1558df34d377f5f7299af2df8c9c768cc979cfdb8f4c1cbc8d161 |
| oracle/views-combined/src/main/java/corpusviewscombined/scopes/boards/writeboards/AddCard.java | 361a742d665dbdcb13e90f4c8337e0ab4ebc9ebb91b31bc8bda913e9412b3694 |
| oracle/views-combined/src/main/java/corpusviewscombined/scopes/boards/writeboards/AddCardHandler.java | d8d4d83e0916da9a38f2a3c78b4e79b9421a4680a6a78d443618f4c4948449a8 |
| oracle/views-combined/src/main/java/corpusviewscombined/scopes/boards/writeboards/ClearDetails.java | b297e7fdad830d53f3dbf8ff9fcbcde1a01512f90cf6c285507a73e33d50575d |
| oracle/views-combined/src/main/java/corpusviewscombined/scopes/boards/writeboards/ClearDetailsHandler.java | ffefa2e542244b3292b14b14eb135da723544b7266d7def54c6d983450ecb278 |
| oracle/views-combined/src/main/java/corpusviewscombined/scopes/boards/writeboards/CreateBoard.java | 5c78cf178e6bb2a666bd42c5363447839417c81af4d578729a6737d17189b4e2 |
| oracle/views-combined/src/main/java/corpusviewscombined/scopes/boards/writeboards/CreateBoardHandler.java | 843e46a490de2d20c65db941eda8bf1920c98f62d6f5873bfdf5bb8285951059 |
| oracle/views-combined/src/main/java/corpusviewscombined/scopes/boards/writeboards/PurgeCard.java | 46418629a1aedd0a4df01a9d3795954ee981f46a8df5700e2b9dedb34e55d80b |
| oracle/views-combined/src/main/java/corpusviewscombined/scopes/boards/writeboards/PurgeCardHandler.java | 2d8920b4bee7db46c1f4526edb7d244155564682a50ccccf692fe3772e042690 |
| oracle/views-combined/src/main/java/corpusviewscombined/scopes/boards/writeboards/RemoveBoard.java | ecf747e5c818c28fa029fe63f6b7024637977dad77381364dc855844829bb4ed |
| oracle/views-combined/src/main/java/corpusviewscombined/scopes/boards/writeboards/RemoveBoardHandler.java | 567ba113ca9b9406b1d1a2afdd0688b1d771fc184d7f2484c143e1115db06f33 |
| oracle/views-combined/src/main/java/corpusviewscombined/scopes/boards/writeboards/RemoveCard.java | f257ba9c58672aed76b2f78331bba0b05d38476a4c99728a6014ae6290b20142 |
| oracle/views-combined/src/main/java/corpusviewscombined/scopes/boards/writeboards/RemoveCardHandler.java | 0d28762dcd9c86e2f69e3df406938694535ee8347dad969e43c582ed2234a668 |
| oracle/views-combined/src/main/java/corpusviewscombined/scopes/boards/writeboards/RenameCard.java | 2e469e825fde2bb631e11972036681ac2f4b7946e15c90b29e04466db30917a9 |
| oracle/views-combined/src/main/java/corpusviewscombined/scopes/boards/writeboards/RenameCardHandler.java | 21a426898e7c62725b6c7deca86608f857c3ce81757a0f8d12390f115b502dfb |
| oracle/views-combined/src/main/java/corpusviewscombined/scopes/events/BoardCreated.java | 78e6c3741be960fc86133e86b0574e0e372bddaddfa0a1c6635e3b3dd089ef8e |
| oracle/views-combined/src/main/java/corpusviewscombined/scopes/events/BoardRemoved.java | c8d62b296e1ceb344faff3f1e1febce0e0eebbe0fd9130fee8161f1583b4854d |
| oracle/views-combined/src/main/java/corpusviewscombined/scopes/events/CardAdded.java | a54f64ae18167b9df1c018f541e92c517b5fc20a3440a9539d8fb588b8a89ee7 |
| oracle/views-combined/src/main/java/corpusviewscombined/scopes/events/CardPurged.java | 1d84a5c1145aa03a70b30f376ba178f14650c9463046932775e82b959076b0e3 |
| oracle/views-combined/src/main/java/corpusviewscombined/scopes/events/CardRemoved.java | a9219e0900ddb9b416dd9badf0c697328a051f5d0a8c56842b4f65ef39bec4bf |
| oracle/views-combined/src/main/java/corpusviewscombined/scopes/events/CardRenamed.java | 244d8d1bb40bfa2174ae29e6388fb469f0e380bb796830e886039716c8cd9073 |
| oracle/views-combined/src/main/java/corpusviewscombined/scopes/events/DetailsRemoved.java | 30d84e979c343fdf8a98e58b60aa0095974941310547be6c436e4d38d5c401a9 |
| oracle/views-combined/src/main/java/corpusviewscombined/scopes/events/EventTags.java | c467756ab6e8d6e9445b77a9449a9e4872043d425ac2acb7d910176a3e03a2d4 |
| oracle/views-combined/src/main/java/corpusviewscombined/views/counters/decrease/Decrease.java | 87ba0033786cdbbc1bee2613076121679953980e28b60cd9412f68d350066ddc |
| oracle/views-combined/src/main/java/corpusviewscombined/views/counters/decrease/DecreaseHandler.java | 1cb3d3b8c6e0053bbfa9bcaab763a415b3c25bf3accf59c9bdc0d6297b6e11c4 |
| oracle/views-combined/src/main/java/corpusviewscombined/views/counters/increase/Increase.java | 5daac528adf4c998bf0c4f4675d2a28c3970cd84b2522193812dbf7cc94e1bd3 |
| oracle/views-combined/src/main/java/corpusviewscombined/views/counters/increase/IncreaseHandler.java | 45dd7643707d727ab7bc706a4e87045c9e8ed15f18002e27633404c06643ae9a |
| oracle/views-combined/src/main/java/corpusviewscombined/views/events/Decreased.java | 70fb392dda4d6fe7745135ecd23060cb7c55522b63af632f42a973939e4c7458 |
| oracle/views-combined/src/main/java/corpusviewscombined/views/events/EventTags.java | d41f3f1d0a02a9cac67aaa6243519c75522898944e8a7427b4d93b3770a9fd46 |
| oracle/views-combined/src/main/java/corpusviewscombined/views/events/Increased.java | d9a94233690bb0ec2199953d4efbf6a446defa9b163da940373266506d684352 |
| oracle/work-orders/src/main/java/corpusworkorders/workorders/board/moveworkorder/MoveWorkOrder.java | 06ca99511d9628b16ffb706db3c370ace7f8e120aee4f535763f8fbd00e92991 |
| oracle/work-orders/src/main/java/corpusworkorders/workorders/board/moveworkorder/MoveWorkOrderHandler.java | 1d71fc636449f5b2e87517a603167c39aa2a5d6f1d3de49c02e30950c83bc7e2 |
| oracle/work-orders/src/main/java/corpusworkorders/workorders/events/EventTags.java | 57cf30ae1963933f47eda3ce8040855781c057cbc417ef7caabd74f2f5d2006f |
| oracle/work-orders/src/main/java/corpusworkorders/workorders/events/WorkOrderMoved.java | 2da21e65be3973651cda13d81571a3de2a3cfe334ed1d95aae322a14f02555dd |
| oracle/work-orders/src/main/java/corpusworkorders/workorders/events/WorkOrderRegistered.java | 8e7375deb8e4b987ac4b0247558710fa4865eda64e1e535251cda68b83930f15 |
| oracle/work-orders/src/main/java/corpusworkorders/workorders/workorders/registerworkorder/RegisterWorkOrder.java | e26885b004bd77a7858dff4ef02254e4bace83879a71b7f7c5423318cad203cd |
| oracle/work-orders/src/main/java/corpusworkorders/workorders/workorders/registerworkorder/RegisterWorkOrderHandler.java | 0b69f60f407cf1b1882e564e9cfceb7c3674a1a95f622006806649fe1c4fafee |
