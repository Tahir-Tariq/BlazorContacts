# Lucene Search Page

Apache lucene based search page written with c# blazor.

## Performance

### Lucene.Net.Store.Azure
```console
Index container size 91108 built in 00:00:08.6260000 | 8626
Search Term 'henderson' returned in 1127 ms  | Total hits: 63| Contains: 1 | Matching: 1 
Search Term 'BrendaRobinson' returned in 2149 ms  | Total hits: 76| Contains: 1 | Matching: 1 
Search Term 'yahoo' returned in 475 ms  | Total hits: 38| Contains: 13 | Matching: 13 
Search Term 'gmail' returned in 403 ms  | Total hits: 59| Contains: 33 | Matching: 33 
Search Term 'ste' returned in 117 ms  | Total hits: 16| Contains: 7 | Matching: 5 
Search Term 'benjamin' returned in 943 ms  | Total hits: 41| Contains: 1 | Matching: 1 
Search Term 'reb' returned in 97 ms  | Total hits: 21| Contains: 1 | Matching: 1 
```

### FSDirectory
```console
Index container size 91108 built in 00:00:00.7550000 | 755
Search Term 'BrendaRobinson' returned in 51 ms  | Total hits: 76| Contains: 1 | Matching: 1 
Search Term 'gmail' returned in 2 ms  | Total hits: 59| Contains: 33 | Matching: 33 
Search Term 'reb' returned in 1 ms  | Total hits: 21| Contains: 1 | Matching: 1 
Search Term 'henderson' returned in 17 ms  | Total hits: 63| Contains: 1 | Matching: 1 
Search Term 'ste' returned in 0 ms  | Total hits: 16| Contains: 7 | Matching: 5 
Search Term 'benjamin' returned in 6 ms  | Total hits: 41| Contains: 1 | Matching: 1 
Search Term 'yahoo' returned in 2 ms  | Total hits: 38| Contains: 13 | Matching: 13 
```