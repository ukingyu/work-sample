# Sort joints by DFS
def jntDFS(root):
    jntStack = [root]
    jntLvStack = [0]
    jntResult = []
    jntLvResult = []
    while (len(jntStack) > 0):
        jnt = jntStack.pop()
        jntLv = jntLvStack.pop()
        
        # Save current joint and its level info
        jntResult.append(jnt)
        jntLvResult.append(jntLv)
        children = cmds.listRelatives(jnt, c=True)
        jntChildren = []
        if (children != None and len(children) > 0):
            for c in children:
                if (cmds.nodeType(c) == 'joint'):
                    jntChildren.append(c)
        
        # If current joint has children
        if (len(jntChildren) > 0):
            for j in jntChildren:
                jntStack.append(j)
                jntLvStack.append(jntLv + 1)
        else:
            jntResult.append('End Site')
            jntLvResult.append(jntLv + 1)
    return jntResult, jntLvResult
            
# Export animation using BVH format    
def BVHExport(filepath, startFrame, endFrame):
    lineBreak = '\r\n'
    indent = '  '
    jntResult, jntLvResult = jntDFS('root')
    
    # Save data of joints
    data = 'HIERARCHY' + lineBreak
    jntData = ''
    phyJntData = ''
    for id in range(len(jntResult)):
        # Save data of root joint
        if (jntResult[id] == 'root'):
            jntData +=    'ROOT root' + lineBreak + '{' + lineBreak
            phyJntData += 'ROOT root' + lineBreak + '{' + lineBreak
            cmds.currentTime(startFrame)
            pos = cmds.xform('root',q=True,t=True,ws=True)
            jntData +=    indent + 'OFFSET ' + format(pos[0], '.6f') + ' ' + format(pos[1], '.6f') + ' ' + format(pos[2], '.6f') + lineBreak
            phyJntData += indent + 'OFFSET ' + format(pos[0], '.6f') + ' ' + format(pos[1], '.6f') + ' ' + format(pos[2], '.6f') + lineBreak 
            jntData +=    indent + 'CHANNELS 6 Xposition Yposition Zposition Zrotation Xrotation Yrotation' + lineBreak
            phyJntData += indent + 'CHANNELS 1 Gyro' + lineBreak
        else: 
            # Save data of End joints
            if (jntResult[id] == 'End Site'):
                jntData +=    multiplyString(indent, jntLvResult[id]) + 'End Site' + lineBreak
                phyJntData += multiplyString(indent, jntLvResult[id]) + 'End Site' + lineBreak
                jntData +=    multiplyString(indent, jntLvResult[id]) + '{' + lineBreak
                phyJntData += multiplyString(indent, jntLvResult[id]) + '{' + lineBreak
                jntData +=    multiplyString(indent, jntLvResult[id]) + indent + 'OFFSET 0.0 0.0 0.0' + lineBreak
                phyJntData += multiplyString(indent, jntLvResult[id]) + indent + 'OFFSET 0.0 0.0 0.0' + lineBreak
                jntData +=    multiplyString(indent, jntLvResult[id]) + '}' + lineBreak
                phyJntData += multiplyString(indent, jntLvResult[id]) + '}' + lineBreak
                # Add '}'
                indentCount = 0
                if (id == len(jntResult) - 1):
                    indentCount = jntLvResult[id]
                if (id < len(jntResult) - 1):
                    indentCount = jntLvResult[id] - jntLvResult[id + 1]
                for i in range(indentCount):
                    jntData +=    multiplyString(indent, jntLvResult[id] - i - 1) + '}' + lineBreak
                    phyJntData += multiplyString(indent, jntLvResult[id] - i - 1) + '}' + lineBreak
            # Save data of all other joints
            else:
                jntData +=    multiplyString(indent, jntLvResult[id]) + 'JOINT ' + jntResult[id] + '\r\n'
                phyJntData += multiplyString(indent, jntLvResult[id]) + 'JOINT ' + jntResult[id] + '\r\n'
                jntData +=    multiplyString(indent, jntLvResult[id]) + '{' + '\r\n'
                phyJntData += multiplyString(indent, jntLvResult[id]) + '{' + '\r\n'
                tx = cmds.getAttr(jntResult[id] + '.tx')
                ty = cmds.getAttr(jntResult[id] + '.ty')
                tz = cmds.getAttr(jntResult[id] + '.tz')
                jntData +=    multiplyString(indent, jntLvResult[id]) + indent + 'OFFSET ' + format(tx, '.6f') + ' ' + format(ty, '.6f') + ' ' + format(tz, '.6f') + lineBreak
                phyJntData += multiplyString(indent, jntLvResult[id]) + indent + 'OFFSET ' + format(tx, '.6f') + ' ' + format(ty, '.6f') + ' ' + format(tz, '.6f') + lineBreak
                jntData +=    multiplyString(indent, jntLvResult[id]) + indent + 'CHANNELS 3 Zrotation Xrotation Yrotation' + lineBreak
                phyJntData += multiplyString(indent, jntLvResult[id]) + indent + 'CHANNELS 2 Kp Kd' + lineBreak
    data += jntData + lineBreak
    
    # Save data of animation
    keyListID = 0
    playSpeed = interpretMayaFPSSetting(cmds.currentUnit(q=True,t=True))
    data += 'MOTION' + lineBreak
    data += 'Frames: ' + str(endFrame - startFrame + 1) + lineBreak
    data += 'Frame Time: ' + format((1.0 / playSpeed), '.7f') + lineBreak
    animData = ''
    phyAnimData = ''
    for key in range(startFrame, (endFrame + 1)):
        cmds.currentTime(key)
        for id in range(len(jntResult)):
            if (jntResult[id] != 'End Site'):
                # Get root's position
                if (jntResult[id] == 'root'):
                    pos = cmds.xform(jntResult[id],q=True,t=True,ws=True)
                    animData += format(pos[0], '.6f') + ' ' + format(pos[1], '.6f') + ' ' + format(pos[2], '.6f') + ' '
                
                # Get every joint's rotation by some extra calculation
                # Store rotate order
                rotOrder = cmds.xform(jntResult[id],q=True,roo=True)
                
                # Force to 'xyz' order
                cmds.xform(jntResult[id],p=True,roo='xyz')
                rot_deg = cmds.xform(jntResult[id],q=True,ro=True,os=True)
                
                # Return to the original order
                cmds.xform(jntResult[id],p=True,roo=rotOrder)
                
                # Degree to radian
                rx_rad = 2 * math.pi * rot_deg[0] / 360
                ry_rad = 2 * math.pi * rot_deg[1] / 360
                rz_rad = 2 * math.pi * rot_deg[2] / 360
                    
                A = math.cos(rx_rad)
                B = math.sin(rx_rad)
                C = math.cos(ry_rad)
                D = math.sin(ry_rad)
                E = math.cos(rz_rad)
                F = math.sin(rz_rad)
                
                # Radian to degree
                rx_deg = math.asin(B * C) * 180 / math.pi
                ry_deg = math.atan2(D, A * C) * 180 / math.pi
                rz_deg = math.atan2(-1 * B * D * E + A * F, B * D * F + A * E) * 180 / math.pi
                animData += format(rz_deg, '.6f') + ' ' + format(rx_deg, '.6f') + ' ' + format(ry_deg, '.6f') + ' '

        animData +=    lineBreak
        phyAnimData += lineBreak
    data += animData + lineBreak
    
    # Save data of physics joints
    data += 'HIERARCHY_MIDAS' + lineBreak
    data += phyJntData + lineBreak
    
    # Save data of physics animation
    keyListID = 0
    data += 'MOTION_MIDAS' + lineBreak
    data += 'Frames: ' + str(endFrame - startFrame + 1) + lineBreak
    data += 'Frame Time: ' + format((1.0 / playSpeed), '.7f') + lineBreak
    data += phyAnimData + lineBreak
    
    # Write data to file
    f = open(filepath, 'w')
    f.write(data)
    f.close()

