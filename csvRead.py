import csv


cutoff = .4

with open('/Users/TLHM/Desktop/abstract_similarities.csv') as csvfile:
    myFile = ""
    reader = csv.reader(csvfile, delimiter=',')
    i=0
    edgeCount=0
    for row in reader:
        
        if(i%200 == 0):
            print("\n"+str(i), end=" ")
        lenC = min(i,len(row))
        for j in range(lenC):
            if(j==i):
                continue
            if(float(row[j]) > cutoff and float(row[j])<1):
                myFile+=str(i)+" "+str(j)+"\n"
                edgeCount = edgeCount+1
                #print(".", end="")
        i=i+1

with open('/Users/TLHM/Desktop/over'+str(cutoff)+'.mtx', 'w') as f:
    f.write(str(i)+"\n"+myFile)

print("done with "+str(edgeCount)+" edges")

