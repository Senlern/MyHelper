#!/bin/bash
#
# Invoked build.xml, overriding the lolua++ property
#!/bin/sh
#!/bin/bash
#取当前路径
CURRENT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)

#编译


echo "当前proto文件目录: "$CURRENT_DIR

function pbmake(){  
	index=1
	cmd="protoc -o main.pb main.proto "
        for file in ` ls $1 `  
        # for file in *.proto
        do  
                # if [ -d $1"/"$file ]  
                # then  
                #         ergodic $1"/"$file  
                # else  
                #         cmd=$cmd$file" "
                # fi 
                affix=${file#*.}
                # echo $affix
                if [ $affix = "proto" ]
                then
                     cmd=$cmd$file" "
                     echo $index":"$file
                     let index++
                fi

                # if [ $file != "*.pb" ] && [ $file != "Naruto.proto" ]
                # 	cmd=$cmd$file" "
                # 	echo $index":"$file
                # 	let index++
                # then
                # fi
        done 
        echo "执行pb生成命令: "$cmd""
        cd $CURRENT_DIR
        $cmd
}  

pbmake $CURRENT_DIR  
