import sys, csv, json

if not len(sys.argv) == 4:
    print("""
        A command-line utility used to transform a .csv map file to
        a JSON file understood by the main application.
        
        Usage: transform.py infile.csv transformfile.json outfile.json""")
    sys.exit(0)
    sys.argv.append("dipl_map.CSV")
    sys.argv.append("dipl_transform.txt")
    sys.argv.append("dipl_output.txt")
    
with open(sys.argv[2]) as trans_file:
    decoder = json.JSONDecoder()
    translate = decoder.decode(trans_file.read())

result = []
missing = 0
with open(sys.argv[1]) as f:
    c = csv.reader(f)
    for line in c:
        l = []
        print(line)
        for cell in line:
            if not cell: continue
            cell = cell.strip()
            try:
                l.append(translate[cell])
            except:
                l.append(cell)
                missing += 1
        result.append(l)

print("The translation dictionary couldn't translate %d items." % missing)
encoder = json.encoder.JSONEncoder()
with open(sys.argv[3], 'w') as outfile:
    outfile.write(encoder.encode(result))
    print("Done")

