# Test traceability

EasyJsonSchema specification v23, section 11. T-11 and T-28 are retired and are not implemented. Each active T-ID, through T-127, has a test method whose name starts with `Tnn_`.

| Test | Method prefix | Requirements and rules |
|---|---|---|
| T-1 | `T01_` | FR-1 |
| T-2 | `T02_` | FR-3, FR-12, D16 |
| T-3 | `T03_` | FR-5, FR-12, D9 |
| T-4 | `T04_` | FR-6, FR-12, D12 |
| T-5 | `T05_` | FR-15, S12 |
| T-6 | `T06_` | FR-8, FR-3, D13, D16 |
| T-7 | `T07_` | FR-16, D5, S4 |
| T-8 | `T08_` | FR-7, D6 |
| T-9 | `T09_` | FR-9, D14 |
| T-10 | `T10_` | FR-19, FR-13, FR-17 |
| T-12 | `T12_` | FR-11, FR-15, D15, S19 |
| T-13 | `T13_` | FR-4, FR-17, D8, D16 |
| T-14 | `T14_` | FR-10, FR-18, D11 |
| T-15 | `T15_` | FR-2, FR-12, D4 |
| T-16 | `T16_` | FR-9, FR-12, D14 |
| T-17 | `T17_` | FR-10, FR-12, D11 |
| T-18 | `T18_` | FR-12, D9, D13 |
| T-19 | `T19_` | FR-13, FR-15, FR-23. Re-runs T-55–T-62, T-65–T-68, T-76–T-81, T-87, T-88, T-93, T-102 |
| T-20 | `T20_` | FR-19. Re-runs T-85 and T-86 |
| T-21 | `T21_` | NFR-1, FR-12, D17 |
| T-22 | `T22_` | FR-21, FR-12, D10 |
| T-23 | `T23_` | FR-3, FR-4, FR-18, D8 |
| T-24 | `T24_` | Section 4.2, FR-7, FR-15, D6, S11 |
| T-25 | `T25_` | FR-22, D7 |
| T-26 | `T26_` | FR-12, section 6, D2, D3 |
| T-27 | `T27_` | FR-19 |
| T-29 | `T29_` | NFR-6 |
| T-30 | `T30_` | Section 6, both `ValidateJsonObject` overloads |
| T-31 | `T31_` | FR-6, D12 |
| T-32 | `T32_` | FR-10, FR-18, D11 |
| T-33 | `T33_` | FR-11, FR-15, S16 |
| T-34 | `T34_` | Section 6. Re-runs the failure cases in T-2–T-9, T-15–T-18, T-21, T-22, T-24–T-26, T-36, T-39, T-42–T-46, T-48, T-51, T-52, T-5, T-33, T-55–T-71, T-74–T-81, T-85–T-89, T-91, T-92, T-93, T-94, T-95, T-97, T-100–T-110 |
| T-35 | `T35_` | FR-19, section 4.2 |
| T-36 | `T36_` | FR-21, FR-12, D10 |
| T-37 | `T37_` | FR-19 |
| T-38 | `T38_` | FR-19 |
| T-39 | `T39_` | Section 4.2, FR-8, FR-11, D15 |
| T-40 | `T40_` | Section 4.2, FR-4, FR-8, D8 |
| T-41 | `T41_` | Section 4.2, FR-4, FR-11, D15 |
| T-42 | `T42_` | Section 4.2, FR-7, FR-22, D6, D7 |
| T-43 | `T43_` | FR-16, D5 |
| T-44 | `T44_` | FR-3, FR-12, D16 |
| T-45 | `T45_` | Section 4.2, FR-12, NFR-1, D17 |
| T-46 | `T46_` | FR-12, section 8, D3 |
| T-47 | `T47_` | Section 6, section 8, D2 |
| T-48 | `T48_` | FR-5, FR-6, D9, D12 |
| T-49 | `T49_` | FR-10, FR-18, D11 |
| T-50 | `T50_` | FR-12 |
| T-51 | `T51_` | FR-12, section 6, D1 |
| T-52 | `T52_` | FR-18, FR-12, section 6 |
| T-53 | `T53_` | FR-13 |
| T-54 | `T54_` | FR-13, FR-16, S4 |
| T-55 | `T55_` | FR-15, S11 |
| T-56 | `T56_` | FR-15, S12 |
| T-57 | `T57_` | FR-15, S13 |
| T-58 | `T58_` | FR-15, S14 |
| T-59 | `T59_` | FR-15, S15 |
| T-60 | `T60_` | FR-11, FR-15, S16 |
| T-61 | `T61_` | FR-15, S18 |
| T-62 | `T62_` | FR-11, FR-15, S19 |
| T-63 | `T63_` | FR-16, S4 |
| T-64 | `T64_` | FR-15, FR-16, S16 |
| T-65 | `T65_` | FR-15, S5 |
| T-66 | `T66_` | FR-12, FR-15, FR-23, S5 |
| T-67 | `T67_` | FR-12, FR-15, S10 |
| T-68 | `T68_` | FR-11, FR-15, S9 |
| T-69 | `T69_` | FR-13, FR-12, S2 |
| T-70 | `T70_` | FR-15, S12, S11 |
| T-71 | `T71_` | FR-15, S10, S11 |
| T-72 | `T72_` | FR-13, FR-16, FR-17 |
| T-73 | `T73_` | FR-19, FR-13 |
| T-74 | `T74_` | FR-3, FR-13, D8, D16 |
| T-75 | `T75_` | Section 6, FR-12, S1 |
| T-76 | `T76_` | FR-12, FR-15, S20 |
| T-77 | `T77_` | FR-12, FR-15, FR-23 |
| T-78 | `T78_` | FR-12, FR-15, FR-22, S8 |
| T-79 | `T79_` | FR-12, FR-15, FR-23, S6 |
| T-80 | `T80_` | FR-15, section 6, S2 |
| T-81 | `T81_` | FR-12, FR-15, FR-23, S7 |
| T-82 | `T82_` | FR-19 |
| T-83 | `T83_` | FR-19 |
| T-84 | `T84_` | FR-19 |
| T-85 | `T85_` | FR-19, FR-12 |
| T-86 | `T86_` | FR-19, FR-16 |
| T-87 | `T87_` | FR-15, FR-12, FR-23, S17 |
| T-88 | `T88_` | FR-15, FR-6, S12 |
| T-89 | `T89_` | FR-6, FR-9, D12, D14 |
| T-90 | `T90_` | FR-19, FR-6, FR-18 |
| T-91 | `T91_` | FR-17 |
| T-92 | `T92_` | FR-19, FR-15, FR-23 |
| T-93 | `T93_` | FR-12, FR-15, FR-23, S5, S7, S10, S19 |
| T-94 | `T94_` | FR-12, FR-15, FR-23 |
| T-95 | `T95_` | NFR-2, FR-6, D12 |
| T-96 | `T96_` | FR-18 |
| T-97 | `T97_` | FR-18, section 8 |
| T-98 | `T98_` | FR-18, FR-6 |
| T-99 | `T99_` | FR-19, FR-6, FR-13 |
| T-100 | `T100_` | FR-16, FR-4, FR-13, D5, D8, S4, S7 |
| T-101 | `T101_` | FR-19, FR-15 |
| T-102 | `T102_` | FR-13, FR-5, FR-23, S6 |
| T-103 | `T103_` | FR-19, section 5.2. Concrete user class is `Object`; abstract classes, interfaces, delegates, and `System` classes are unsupported |
| T-104 | `T104_` | FR-19, FR-18. Rank-1 arrays and `List<byte>` map to the array form; `byte[]` is unsupported |
| T-105 | `T105_` | FR-19. Types outside the section 5.2 table are unsupported |
| T-106 | `T106_` | FR-19, FR-7. `[JsonIgnore]` Always and WhenReading, and static properties, are excluded |
| T-107 | `T107_` | FR-19, section 6. `$: type is null` |
| T-108 | `T108_` | FR-17, FR-18. JSON literal `null` cannot be deserialized |
| T-109 | `T109_` | FR-18, section 6. `InvalidOperationException` maps to `cannot deserialize to <TypeName>` |
| T-110 | `T110_` | 4.1, D2, D3, S1. JSON whitespace only; U+00A0 is malformed JSON |
| T-111 | `T111_` | FR-24, FR-19, FR-13, FR-17. Compact and indented generation |
| T-112 | `T112_` | FR-24, FR-19. Indented `GenerateEasyJsonSchema(Type)` |
| T-113 | `T113_` | FR-25, FR-18. Serialize the FR-19 user |
| T-114 | `T114_` | FR-25, FR-19. Enum member JSON names |
| T-115 | `T115_` | FR-25, D10. Number and bool formatting |
| T-116 | `T116_` | FR-25, D11, FR-18. DateTime kinds and local offset |
| T-117 | `T117_` | FR-25, D14. Arrays and lists |
| T-118 | `T118_` | FR-25, FR-19. `[JsonIgnore]` conditions are written when FR-19 includes them |
| T-119 | `T119_` | FR-25, D6. Names and default escaping |
| T-120 | `T120_` | FR-25, FR-24. Compact and indented object JSON |
| T-121 | `T121_` | FR-25, 4.1, section 6. Null, non-finite numbers, unnamed enum |
| T-122 | `T122_` | FR-19. Flags, converter, number handling, and polymorphic types |
| T-123 | `T123_` | FR-25, D8, D14. Output that fails the generated schema |
| T-124 | `T124_` | FR-19, FR-25. Collection classes |
| T-125 | `T125_` | FR-19, FR-25, D6. Public get and set, or init |
| T-126 | `T126_` | FR-25, FR-19. Serialize uses the generation check |
| T-127 | `T127_` | D6, D7, FR-7, FR-22. Escaped data keys |

`Nfr5_HotPathBenchmark_IsReported` reports the hot-path measurement for NFR-5. It is a report, not a pass/fail gate. The success path stays at 0 allocated bytes per operation. `ScalarRangesAndMetadata_AreAccepted` covers the rest of the section 5.2 numeric ranges and the optional schema fields. `Section11TestMethods_ExistForActiveTestIds` checks that every active T-ID has a method.
